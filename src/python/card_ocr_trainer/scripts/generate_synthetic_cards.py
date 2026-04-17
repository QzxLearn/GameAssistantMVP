import cv2
import numpy as np
import os
from pathlib import Path
import random
import string

# 从配置加载路径
import yaml
with open('../config/paths.yaml') as f:
    cfg = yaml.safe_load(f)
    raw_dir = Path(cfg['raw_dir'].replace('${USER}', os.getenv('USER')))

raw_dir.mkdir(parents=True, exist_ok=True)

# 合成卡牌名称（SlayTheSpire风格）
card_names = ["Strike", "Defend", "Bash", "Pummel", "Flex", "Iron Wave"]
fonts = [cv2.FONT_HERSHEY_SIMPLEX, cv2.FONT_HERSHEY_DUPLEX]

for i in range(50):  # 生成50张合成卡牌
    # 创建空白卡牌背景
    card = np.zeros((300, 250, 3), dtype=np.uint8)
    card[:] = (30, 25, 40)  # 暗色背景
    
    # 随机选择卡牌名称
    name = random.choice(card_names)
    
    # 绘制艺术字体效果（模拟游戏卡牌）
    font = random.choice(fonts)
    font_scale = 1.2 + random.random() * 0.3
    thickness = 2 + random.randint(0, 1)
    
    # 白色主文本
    cv2.putText(card, name, (30, 80), font, font_scale, (255, 255, 255), thickness)
    
    # 蓝色描边（增强艺术感）
    for dx, dy in [(-1,-1), (1,-1), (-1,1), (1,1)]:
        cv2.putText(card, name, (30+dx, 80+dy), font, font_scale, (60, 120, 255), thickness)
    
    # 添加轻微噪声模拟截图质量
    noise = np.random.normal(0, 5, card.shape).astype(np.uint8)
    card = cv2.add(card, noise)
    
    # 保存到共享目录（C#可访问）
    cv2.imwrite(str(raw_dir / f"synthetic_{i:03d}.png"), card)
    print(f"生成: synthetic_{i:03d}.png -> {name}")

print(f"\n✅ 合成数据生成完成！共50张卡牌")
print(f"📁 保存路径: {raw_dir}")