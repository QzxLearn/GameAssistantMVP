using GameAssistantMVP.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistantMVP.Services
{
    public interface IScreenCaptureService
    {
        /// <summary>
        /// 截取全屏
        /// </summary>
        /// <returns></returns>
        Mat CaptureFullscreen();
        
        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        Mat CaptureRegion(CaptureRegion region);

        /// <summary>
        /// 截取指定坐标和尺寸的区域（兼容旧调用方式）
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Mat CaptureRegion(double x, double y, double width, double height);
    }
}
