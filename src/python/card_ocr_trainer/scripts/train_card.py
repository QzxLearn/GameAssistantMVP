#!/usr/bin/env python3
"""
一键训练脚本 - 最小化版本
用法：python scripts/train_card.py --epochs 50
"""
import os
import sys
import argparse
import yaml
from pathlib import Path

# 添加项目路径
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.core.path_resolver import paths

def main():
    # 确保目录存在
    paths.ensure_directories()
    
    print(f"📁 共享数据目录：{paths.shared_data_root}")
    print(f"📁 模型目录：{paths.models_dir}")
    print(f"📁 原始数据目录：{paths.raw_dir}")
    print(f"📁 标注目录：{paths.labeled_dir}")
    
    # 检查 C# 是否已生成数据
    raw_images = list(paths.raw_dir.glob("*.png"))
    if len(raw_images) == 0:
        print("⚠️ 警告：未找到原始数据！")
        print("💡 请先使用 C# WpfClient 采集卡牌截图")
        print(f"📂 数据应保存在：{paths.raw_dir}")
        return
    
    print(f"✅ 找到 {len(raw_images)} 张原始图片")

import torch
import torch.nn as nn
from torch.utils.data import DataLoader
from torch.optim import Adam
from src.core.model import create_model
from src.core.dataset import CardNameDataset, collate_fn

def load_config():
    config_path = Path(__file__).parent.parent / "config" / "training.yaml"
    with open(config_path, 'r', encoding='utf-8') as f:
        return yaml.safe_load(f)

def train():
    parser = argparse.ArgumentParser()
    parser.add_argument('--epochs', type=int, default=None)
    parser.add_argument('--batch_size', type=int, default=None)
    args = parser.parse_args()
    
    # 加载配置
    config = load_config()
    
    # 覆盖配置
    if args.epochs:
        config['training']['epochs'] = args.epochs
    if args.batch_size:
        config['training']['batch_size'] = args.batch_size
    
    # 设置设备
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    print(f"🔥 使用设备：{device}")
    
    # 创建模型
    model = create_model(
        num_classes=config['model']['num_classes'],
        hidden_size=config['model']['hidden_size']
    ).to(device)
    
    # 加载数据
    labeled_dir = Path(config['data']['labeled_dir'])
    if not labeled_dir.exists():
        print(f"⚠️ 标注目录不存在：{labeled_dir}")
        print("💡 请先使用 C# 采集数据并标注")
        # 创建空标注文件
        labeled_dir.mkdir(parents=True, exist_ok=True)
        (labeled_dir / "annotations.json").write_text('{"cards": []}', encoding='utf-8')
    
    dataset = CardNameDataset(str(labeled_dir))
    
    if len(dataset) == 0:
        print("❌ 没有训练数据！请先采集至少 10 张卡牌并标注")
        print("💡 使用 C# WpfClient 的 TrainingDataCaptureWindow 采集数据")
        return
    
    dataloader = DataLoader(
        dataset,
        batch_size=config['training']['batch_size'],
        shuffle=True,
        collate_fn=collate_fn
    )
    
    # 损失函数 (CTC Loss)
    criterion = nn.CTCLoss(blank=0, zero_infinity=True)
    optimizer = Adam(model.parameters(), lr=config['training']['learning_rate'])
    
    # 训练循环
    print(f"🚀 开始训练：{config['training']['epochs']} epochs, {len(dataset)} 样本")
    
    for epoch in range(config['training']['epochs']):
        model.train()
        total_loss = 0
        
        for batch_idx, batch in enumerate(dataloader):
            images = batch['images'].to(device)
            targets = batch['targets'].to(device)
            target_lengths = batch['target_lengths'].to(device)
            
            # 计算输入长度 (经过 CNN 后的序列长度)
            input_lengths = torch.full(
                (images.size(0),), 
                images.size(-1) // 4,  # 根据 CNN 下采样率计算
                dtype=torch.long
            ).to(device)
            
            optimizer.zero_grad()
            
            # 前向传播
            output = model(images)  # (batch, seq_len, num_classes)
            
            # CTC Loss 需要 (seq_len, batch, num_classes)
            output = output.log_softmax(2).permute(1, 0, 2)
            
            loss = criterion(output, targets, input_lengths, target_lengths)
            loss.backward()
            
            optimizer.step()
            total_loss += loss.item()
        
        avg_loss = total_loss / len(dataloader)
        
        if (epoch + 1) % 10 == 0 or epoch == 0:
            print(f"📊 Epoch {epoch+1}/{config['training']['epochs']} - Loss: {avg_loss:.4f}")
    
    # 保存模型
    models_dir = Path(config['data']['models_dir'])
    models_dir.mkdir(parents=True, exist_ok=True)
    
    # 保存 PyTorch 模型
    torch_path = models_dir / "card_name.pth"
    torch.save(model.state_dict(), torch_path)
    print(f"✅ PyTorch 模型保存：{torch_path}")
    
    # 导出 ONNX
    export_onnx(model, models_dir / "card_name.onnx", config)
    
    print("🎉 训练完成！")

def export_onnx(model, onnx_path, config):
    """导出 ONNX 模型"""
    model.eval()
    
    # 创建示例输入
    dummy_input = torch.randn(
        1, 
        1, 
        config['model']['input_height'], 
        config['model']['input_width']
    )
    
    # 导出
    torch.onnx.export(
        model,
        dummy_input,
        str(onnx_path),
        input_names=['input'],
        output_names=['output'],
        dynamic_axes={
            'input': {0: 'batch'},
            'output': {0: 'batch'}
        },
        opset_version=13
    )
    
    print(f"✅ ONNX 模型导出：{onnx_path}")
    
    # 验证 ONNX
    try:
        import onnxruntime as ort
        session = ort.InferenceSession(str(onnx_path))
        print("✅ ONNX 模型验证成功")
    except Exception as e:
        print(f"⚠️ ONNX 验证失败：{e}")

if __name__ == '__main__':
    train()