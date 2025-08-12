# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CurveMaster is a Unity 6 project (version 6000.0.29f1) - a comprehensive curve/spline editing system with multiple spline implementations and intuitive Scene view editing tools.

## System Philosophy

### 設計原則
- **極簡介面**: 所有功能都透過簡單直覺的操作完成
- **模組化架構**: 透過介面(ISpline, ISplineFollower)實現可擴充性
- **效能優先**: 使用快取機制避免重複計算
- **Transform 支援**: 正確處理父節點的旋轉與縮放

### 核心架構
```
Core/
├── ISpline          # 曲線介面
├── ISplineFollower  # 跟隨者介面
├── BaseSpline       # 基礎實作
└── SplineType       # 曲線類型枚舉

Splines/
├── BSpline          # B-Spline 實作
├── CatmullRomSpline # Catmull-Rom 實作
├── CubicSpline      # 三次樣條實作
└── BezierSpline     # 貝茲曲線實作

Components/
├── SplineManager    # 主控制器 (管理曲線切換)
├── SplineControlPoint # 控制點元件
└── SplineCursor     # 游標元件 (0-1 位置控制)

Movement/
├── SplineMovement   # 移動行為基類
└── ConstantSpeedMovement # 恆速移動實作

Editor/
├── SplineManagerEditor # 曲線管理器編輯器
└── SplineCursorEditor  # 游標編輯器
```

## 使用指南

### 建立曲線
1. 建立空物件並加入 SplineManager 元件
2. 建立子物件作為控制點，加入 SplineControlPoint 元件
3. 在 Inspector 選擇曲線類型 (BSpline/CatmullRom/CubicSpline/BezierSpline)
4. 在 Scene 視圖中拖曳控制點調整曲線形狀

### 加入游標
1. 在 Spline 物件下建立子物件
2. 加入 SplineCursor 元件
3. 調整 Position (0-1) 滑桿即可看到游標沿曲線移動
4. 勾選 Align To Tangent 讓游標對齊曲線切線

### 擴充移動行為
```csharp
// 繼承 SplineMovement 建立自訂移動行為
public class ProjectileMovement : SplineMovement
{
    protected override void UpdateMovement()
    {
        // 實作自訂移動邏輯
    }
}
```

## 開發重點

### 效能優化
- SplineCache 類別提供點快取機制
- 只在控制點變更時重新計算曲線
- 使用物件池管理控制點

### 編輯器工具
- Scene 視圖即時預覽曲線
- Handles 直接拖曳控制點
- 根據曲線類型顯示不同控制器
- Inspector 提供完整參數調整

### 程式碼規範
- 所有註解使用繁體中文
- 保持程式碼極簡易懂
- 介面定義清晰分離
- 使用 namespace 組織程式碼

## Unity 專案設定

- **Unity Version**: 6000.0.29f1 (Unity 6)
- **Render Pipeline**: URP 17.0.3
- **Input System**: 1.11.2
- **主要目錄**: Assets/CurveMaster/Script/

## 測試與偵錯

在 Unity Editor 中：
1. 開啟 SampleScene
2. 選擇 [Spline] 物件查看曲線設定
3. 調整 SplineCursor 的 Position 測試移動
4. 切換不同 SplineType 測試曲線類型轉換
- 程式碼註解以及git commit message請都使用台灣用語的繁體中文。程式碼盡量極簡，簡單好懂，滿足需求。