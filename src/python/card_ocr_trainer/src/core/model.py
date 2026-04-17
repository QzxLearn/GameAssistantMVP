"""
最小化 CRNN 模型 - 卡牌名称识别
输入：(batch, 1, 32, 128) 灰度图
输出：(batch, seq_len, num_classes) CTC logits
"""
import torch
import torch.nn as nn

class CRNNModel(nn.Module):
    def __init__(self, num_classes=37, hidden_size=256):
        super().__init__()
        
        # CNN 特征提取 (简化版 VGG)
        self.cnn = nn.Sequential(
            # Block 1: 32x128 -> 16x64
            nn.Conv2d(1, 64, 3, 1, 1),
            nn.BatchNorm2d(64),
            nn.ReLU(True),
            nn.MaxPool2d(2, 2),
            
            # Block 2: 16x64 -> 8x32
            nn.Conv2d(64, 128, 3, 1, 1),
            nn.BatchNorm2d(128),
            nn.ReLU(True),
            nn.MaxPool2d(2, 2),
            
            # Block 3: 8x32 -> 4x16 (只压缩高度)
            nn.Conv2d(128, 256, 3, 1, 1),
            nn.BatchNorm2d(256),
            nn.ReLU(True),
            nn.MaxPool2d((2, 1), (2, 1)),  # 高度减半，宽度不变
            
            # Block 4: 4x16 -> 2x16
            nn.Conv2d(256, 256, 3, 1, 1),
            nn.BatchNorm2d(256),
            nn.ReLU(True),
            nn.MaxPool2d((2, 1), (2, 1)),
        )
        
        # RNN 序列建模
        self.rnn = nn.LSTM(
            input_size=256 * 2,  # 高度=2 时的特征维度
            hidden_size=hidden_size,
            num_layers=2,
            batch_first=True,
            bidirectional=True
        )
        
        # 分类头
        self.fc = nn.Linear(hidden_size * 2, num_classes)
        
    def forward(self, x):
        # x: (batch, 1, 32, 128)
        features = self.cnn(x)  # (batch, 256, 2, 16)
        
        # 展平高度维度，转为序列
        batch, channels, height, width = features.shape
        features = features.permute(0, 3, 1, 2).contiguous()  # (batch, width, channels, height)
        features = features.view(batch, width, channels * height)  # (batch, 16, 512)
        
        # LSTM 序列处理
        rnn_out, _ = self.rnn(features)  # (batch, 16, 512)
        
        # 分类
        output = self.fc(rnn_out)  # (batch, 16, num_classes)
        return output


def create_model(num_classes=37, hidden_size=256):
    return CRNNModel(num_classes=num_classes, hidden_size=hidden_size)