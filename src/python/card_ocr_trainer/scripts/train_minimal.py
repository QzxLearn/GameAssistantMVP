import os
os.environ['TORCH_ONNX_USE_LEGACY_EXPORTER'] = '1'

import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import Dataset, DataLoader
import cv2
import numpy as np
from pathlib import Path
import re
import yaml

# ===== 1. 安全加载配置 =====
with open('../config/paths.yaml') as f:
    cfg_raw = yaml.safe_load(f)

def expand_path(path_str: str) -> str:
    path_str = re.sub(r'\$\{(\w+)\}', lambda m: os.getenv(m.group(1), ''), path_str)
    path_str = re.sub(r'\$(\w+)', lambda m: os.getenv(m.group(1), ''), path_str)
    return path_str

cfg = {k: Path(expand_path(v)) for k, v in cfg_raw.items()}
cfg['models_dir'].mkdir(parents=True, exist_ok=True)

# ===== 2. 简化模型：移除LSTM，改用CNN+固定长度分类 =====
class SimpleCardOCR(nn.Module):
    def __init__(self, num_classes=28, max_chars=12):
        super().__init__()
        self.max_chars = max_chars
        
        self.cnn = nn.Sequential(
            nn.Conv2d(1, 32, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Conv2d(32, 64, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Conv2d(64, 128, kernel_size=3, padding=1),
            nn.ReLU(),
        )
        self.width_pool = nn.AdaptiveAvgPool2d((None, max_chars))
        self.height_pool = nn.AdaptiveAvgPool2d((1, None))
        self.char_classifier = nn.Conv2d(128, num_classes, kernel_size=1)
    
    def forward(self, x):
        x = self.cnn(x)  # [B, 128, 20, 50]
        x = self.width_pool(x)  # [B, 128, 20, 12]
        x = self.height_pool(x)  # [B, 128, 1, 12]
        x = self.char_classifier(x)  # [B, 28, 1, 12]
        x = x.squeeze(2).permute(0, 2, 1).contiguous()  # ✅ 确保连续性
        return x

# ===== 3. 数据集 =====
class SyntheticCardDataset(Dataset):
    def __init__(self, img_dir):
        self.images = list(img_dir.glob("synthetic_*.png"))
        self.chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ "
    
    def __len__(self):
        return len(self.images)
    
    def __getitem__(self, idx):
        img = cv2.imread(str(self.images[idx]), cv2.IMREAD_GRAYSCALE)
        img = cv2.resize(img, (200, 80))
        img = img.astype(np.float32) / 255.0
        img = img[np.newaxis, :, :]
        return torch.tensor(img), 0

# ===== 4. 训练流程（关键修复：reshape替代view）=====
dataset = SyntheticCardDataset(cfg['raw_dir'])
dataloader = DataLoader(dataset, batch_size=8, shuffle=True)

model = SimpleCardOCR()
optimizer = optim.Adam(model.parameters(), lr=0.001)
criterion = nn.CrossEntropyLoss()

print("🚀 开始训练验证（2个epoch）...")
for epoch in range(2):
    for i, (images, _) in enumerate(dataloader):
        optimizer.zero_grad()
        outputs = model(images)  # [B, 12, 28]
        
        # 生成随机目标（仅验证流程）
        target = torch.randint(0, 28, (images.size(0), 12))
        
        # ✅ 关键修复：用reshape替代view
        loss = criterion(outputs.reshape(-1, 28), target.reshape(-1))
        
        loss.backward()
        optimizer.step()
        
        if i % 5 == 0:
            print(f"Epoch {epoch+1}/2, Batch {i}/{len(dataloader)}, Loss: {loss.item():.4f}")

# ===== 5. 导出ONNX =====
model.eval()
dummy_input = torch.randn(1, 1, 80, 200)
onnx_path = cfg['models_dir'] / "card_name_simple.onnx"

torch.onnx.export(
    model,
    dummy_input,
    onnx_path,
    input_names=["input"],
    output_names=["output"],
    opset_version=11,
    export_params=True,
    do_constant_folding=True,
)

print(f"\n✅ ONNX模型导出成功（opset 11）！")
print(f"📁 路径: {onnx_path}")
print(f"💡 模型输出: [batch, 12, 28] - 12个字符位置，每个位置28类概率")