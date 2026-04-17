#!/usr/bin/env python3
"""
推理测试脚本 - 验证 ONNX 模型
用法：python scripts/test_inference.py --image path/to/card.png
"""
import sys
import argparse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

import numpy as np
import cv2
import onnxruntime as ort
from src.preprocess.align_with_csharp import load_and_preprocess

CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ "

def ctc_decode(logits: np.ndarray) -> str:
    """简化 CTC 解码"""
    result = []
    prev = -1
    
    for timestep in logits:
        max_idx = np.argmax(timestep)
        if max_idx != prev and max_idx != 0:
            result.append(CHARS[max_idx - 1])
        prev = max_idx
    
    return ''.join(result)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--image', type=str, required=True)
    parser.add_argument('--model', type=str, default=None)
    args = parser.parse_args()
    
    # 模型路径
    if args.model is None:
        model_path = Path(__file__).parent.parent / "data" / "models" / "card_name.onnx"
    else:
        model_path = Path(args.model)
    
    if not model_path.exists():
        print(f"❌ 模型不存在：{model_path}")
        print("💡 请先运行：python scripts/train_card.py")
        return
    
    # 加载模型
    session = ort.InferenceSession(str(model_path))
    print(f"✅ 加载模型：{model_path}")
    
    # 加载并预处理图像
    try:
        input_tensor = load_and_preprocess(args.image)
    except Exception as e:
        print(f"❌ 图像加载失败：{e}")
        return
    
    # 推理
    outputs = session.run(None, {'input': input_tensor})
    logits = outputs[0][0]  # (seq_len, num_classes)
    
    # 解码
    text = ctc_decode(logits)
    print(f"📝 识别结果：{text}")

if __name__ == '__main__':
    main()