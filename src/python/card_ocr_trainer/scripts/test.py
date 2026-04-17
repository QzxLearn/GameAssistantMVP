import onnxruntime as ort
import numpy as np

model_path = "/mnt/c/Users/qzx/AppData/Local/GameAssistant/data/models/card_name_simple.onnx"
sess = ort.InferenceSession(model_path)

# 推理测试
input_name = sess.get_inputs()[0].name
output_name = sess.get_outputs()[0].name
dummy = np.random.randn(1, 1, 80, 200).astype(np.float32)
result = sess.run([output_name], {input_name: dummy})

print(f"✅ 推理成功！输出形状: {result[0].shape}")
print(f"💡 期望形状: (1, 12, 28) - 12个字符位置，28类概率分布")