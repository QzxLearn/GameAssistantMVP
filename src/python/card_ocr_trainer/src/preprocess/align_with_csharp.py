"""
⚠️ 关键：预处理必须与 C# ImagePreprocessor.cs 完全一致！
"""
import cv2
import numpy as np

def preprocess_for_ocr(image: np.ndarray, target_width=128, target_height=32) -> np.ndarray:
    """
    与 C# ImagePreprocessor.PreprocessForCardText 对齐
    
    C# 逻辑：
    1. 转灰度
    2. 高斯模糊 (3x3)
    3. 自适应阈值 (GaussianC, BinaryInv, 11, 3)
    4. 形态学闭运算 (2x2)
    5. Resize (3x Lanczos4)
    6. 最终 Resize 到目标尺寸
    """
    # 1. 转灰度
    if len(image.shape) == 3:
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    else:
        gray = image.copy()
    
    # 2. 高斯模糊去噪
    denoised = cv2.GaussianBlur(gray, (3, 3), 0)
    
    # 3. 自适应阈值（反色：黑底白字）
    binary = cv2.adaptiveThreshold(
        denoised, 255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY_INV,
        11, 3
    )
    
    # 4. 形态学闭运算（连接断裂文字）
    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (2, 2))
    morphed = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel)
    
    # 5. 放大 3x（与 C# 一致）
    scaled = cv2.resize(morphed, None, fx=3.0, fy=3.0, interpolation=cv2.INTER_CUBIC)
    
    # 6. 最终 Resize 到模型输入尺寸
    final = cv2.resize(scaled, (target_width, target_height), interpolation=cv2.INTER_CUBIC)
    
    # 7. 归一化到 0-1 (与 C# MatToTensor 一致)
    normalized = final.astype(np.float32) / 255.0
    
    # 8. 添加通道维度 (batch, channel, height, width)
    tensor = np.expand_dims(normalized, axis=(0, 1))
    
    return tensor


def load_and_preprocess(image_path: str, target_width=128, target_height=32) -> np.ndarray:
    """加载图像并预处理"""
    image = cv2.imread(image_path)
    if image is None:
        raise ValueError(f"无法加载图像：{image_path}")
    return preprocess_for_ocr(image, target_width, target_height)