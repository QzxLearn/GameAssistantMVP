using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistantMVP.Models
{
    /// <summary>
    /// 表示一个屏幕上的矩形选择区域 （单位：像素，屏幕坐标系）
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <param name="Width"></param>
    /// <param name="Height"></param>
    public record CaptureRegion(double X, double Y, double Width, double Height);
}
