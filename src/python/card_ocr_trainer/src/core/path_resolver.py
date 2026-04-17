"""
路径解析器 - 自动检测 WSL/原生 Linux 环境
"""
import os
from pathlib import Path
import yaml

class PathResolver:
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialized = False
        return cls._instance
    
    def __init__(self):
        if self._initialized:
            return
        
        self._initialized = True
        self.config = self._load_config()
        self.is_wsl = self._detect_wsl()
    
    def _detect_wsl(self) -> bool:
        """检测是否在 WSL 环境中"""
        try:
            with open('/proc/version', 'r') as f:
                return 'microsoft' in f.read().lower()
        except:
            return False
    
    def _load_config(self) -> dict:
        """加载路径配置"""
        config_path = Path(__file__).parent.parent.parent / "config" / "paths.yaml"
        if config_path.exists():
            with open(config_path, 'r', encoding='utf-8') as f:
                return yaml.safe_load(f)
        return {}
    
    @property
    def shared_data_root(self) -> Path:
        """获取共享数据根目录"""
        if self.is_wsl:
            # WSL 环境：使用符号链接路径
            base = Path(__file__).parent.parent.parent / self.config['shared_data']['root']
            return base.resolve()
        else:
            # 原生 Linux：直接配置路径
            return Path(self.config.get('shared_data', {}).get('root', './data'))
    
    @property
    def models_dir(self) -> Path:
        """模型输出目录"""
        return self.shared_data_root / "models"
    
    @property
    def raw_dir(self) -> Path:
        """原始数据目录"""
        return self.shared_data_root / "raw"
    
    @property
    def labeled_dir(self) -> Path:
        """标注数据目录"""
        return self.shared_data_root / "labeled"
    
    def ensure_directories(self):
        """确保所有目录存在"""
        for dir_path in [self.models_dir, self.raw_dir, self.labeled_dir]:
            dir_path.mkdir(parents=True, exist_ok=True)
            print(f"✅ 确保目录存在：{dir_path}")

# 全局单例
paths = PathResolver()