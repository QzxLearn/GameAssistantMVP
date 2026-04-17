"""
最小化数据集 - 从标注 JSON 加载
"""
import json
import os
from pathlib import Path
import cv2
import numpy as np
from torch.utils.data import Dataset

# 字符映射（与 CTC 解码一致）
CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ "

class CardNameDataset(Dataset):
    def __init__(self, labeled_dir: str, max_length=20):
        self.labeled_dir = Path(labeled_dir)
        self.max_length = max_length
        self.samples = []
        
        # 加载标注文件
        annotations_file = self.labeled_dir / "annotations.json"
        if annotations_file.exists():
            with open(annotations_file, 'r', encoding='utf-8') as f:
                annotations = json.load(f)
            
            for item in annotations.get('cards', []):
                if item.get('verified', False):  # 只使用已验证的标注
                    self.samples.append({
                        'image': str(self.labeled_dir / 'images' / item['image']),
                        'text': item['text'].upper().strip()  # 统一大写
                    })
        
        print(f"📦 加载数据集：{len(self.samples)} 个样本")
    
    def __len__(self):
        return len(self.samples)
    
    def __getitem__(self, idx):
        sample = self.samples[idx]
        
        # 加载图像
        image = cv2.imread(sample['image'], cv2.IMREAD_GRAYSCALE)
        if image is None:
            # 返回空白图像作为占位
            image = np.zeros((32, 128), dtype=np.uint8)
        
        # 预处理
        image = cv2.resize(image, (128, 32), interpolation=cv2.INTER_CUBIC)
        image = image.astype(np.float32) / 255.0
        
        # 编码文本为 CTC 标签
        labels = self.encode_text(sample['text'])
        
        return {
            'image': np.expand_dims(image, axis=0),  # (1, 32, 128)
            'labels': labels,
            'text': sample['text']
        }
    
    def encode_text(self, text: str) -> list:
        """文本转 CTC 标签索引"""
        return [CHARS.index(c) + 1 for c in text if c in CHARS]  # 0 保留给 blank
    
    @staticmethod
    def decode_labels(labels: list) -> str:
        """CTC 标签转文本（简化版，去重）"""
        result = []
        prev = -1
        for label in labels:
            if label != prev and label != 0:
                result.append(CHARS[label - 1])
            prev = label
        return ''.join(result)


def collate_fn(batch):
    """自定义 collate 处理变长序列"""
    images = torch.FloatTensor([item['image'] for item in batch])
    texts = [item['text'] for item in batch]
    
    # CTC 需要标签长度
    label_lengths = torch.IntTensor([len(item['labels']) for item in batch])
    # 拼接所有标签
    all_labels = torch.IntTensor([l for item in batch for l in item['labels']])
    
    return {
        'images': images,
        'targets': all_labels,
        'target_lengths': label_lengths,
        'texts': texts
    }


# 需要 torch 导入
import torch