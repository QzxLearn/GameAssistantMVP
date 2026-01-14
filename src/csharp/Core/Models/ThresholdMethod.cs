using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 图像二值化阈值方法（用于 OCR 预处理）
    /// </summary>
    public enum ThresholdMethod
    {
        /// <summary>
        /// 不进行二值化
        /// </summary>
        None,

        /// <summary>
        /// 自动 Otsu 阈值（推荐用于文本）
        /// </summary>
        Otsu,

        /// <summary>
        /// 固定阈值（需配合 ThresholdValue 使用）
        /// </summary>
        Binary,

        /// <summary>
        /// 自适应高斯阈值（暂不实现，预留）
        /// </summary>
        AdaptiveGaussian
    }
}
