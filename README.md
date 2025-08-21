# CurveMaster

A comprehensive spline/curve system for Unity, extracted and reimplemented from the boss "Ji" flying sword mechanics in the game **Nine Sols**. This repository contains a clean, reusable implementation created with Claude Code.

![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Showcase

### Original Nine Sols Implementation
The flying sword mechanics from boss "Ji" in Nine Sols - the inspiration for this system:

![Nine Sols Ji Boss Fight](docs/images/ji-ninesols-gameplay.gif)

### Nine Sols Editor
How developers edit the flying sword spline paths in the Nine Sols editor:

![Nine Sols Editor](docs/images/ji-ninesols-editor.gif)

### CurveMaster in Action
Our reimplemented system running in Unity:

![CurveMaster Demo](docs/images/curvemaster-demo.gif)

## Quick Start

### Installation via Unity Package Manager

1. Open Unity Package Manager (Window → Package Manager)
2. Click the **+** button and select **"Add package from git URL..."**
3. Enter: `https://github.com/RED-CANDLE-GAMES-CO-LTD/CurveMaster.git`
4. Click **Add**

### Basic Usage

#### Option 1: Quick Setup
1. Check out `Assets/CurveMaster/Samples/SampleScene.unity` for a complete example
2. Simply drag the `[Spline].prefab` into your scene and you're ready to go!

#### Option 2: Manual Setup
1. Create an empty GameObject
2. Add the `SplineManager` component
3. Create child GameObjects as control points
4. Add `SplineControlPoint` component to each child
5. Adjust control points in Scene view to shape your curve

## Features

- **Multiple Spline Types**: Bezier, B-Spline, Catmull-Rom, and Cubic Spline implementations
- **Visual Scene Editing**: Intuitive handles and gizmos for editing curves directly in Scene view
- **Dynamic Target Tracking**: Control points can track moving targets in real-time
- **Shape Preservation**: Maintains curve shape when endpoints move
- **Modular Architecture**: Clean interface-based design (ISpline, ISplineFollower)
- **Performance Optimized**: Built-in caching system to avoid redundant calculations
- **Transform Support**: Proper handling of parent rotations and scaling

## Core Components

### SplineManager
The main controller that manages spline switching and control points.

```csharp
SplineManager splineManager = GetComponent<SplineManager>();
splineManager.SwitchSplineType(SplineType.BezierSpline);
Vector3 pointOnCurve = splineManager.GetPoint(0.5f); // Get midpoint
```

### SplineControlPoint
Component for individual control points. Automatically registers with parent SplineManager.

### SplineCursor
Allows objects to follow along the spline path.

```csharp
SplineCursor cursor = GetComponent<SplineCursor>();
cursor.Position = 0.5f; // Move to middle of spline (0-1)
cursor.AlignToTangent = true; // Rotate along curve direction
```

### SplineTargetTracker
Makes control points track target objects with various tracking modes.

```csharp
SplineTargetTracker tracker = GetComponent<SplineTargetTracker>();
tracker.SetupTracking(targetTransform, TrackingMode.Smooth, speed: 5f);
```

### SplineShapeKeeper
Maintains curve shape when endpoints are being tracked.

```csharp
SplineShapeKeeper shapeKeeper = GetComponent<SplineShapeKeeper>();
shapeKeeper.shapeMode = ShapeMode.Elastic; // or Rigid
```

## Architecture

```
Core/
├── ISpline              # Spline interface
├── ISplineFollower      # Follower interface  
├── BaseSpline           # Base implementation
└── SplineType           # Spline type enum

Splines/
├── BezierSpline         # Bezier curve implementation
├── BSpline              # B-Spline implementation
├── CatmullRomSpline     # Catmull-Rom implementation
└── CubicSpline          # Cubic spline implementation

Components/
├── SplineManager        # Main controller
├── SplineControlPoint   # Control point component
├── SplineCursor         # Cursor component (0-1 position)
├── SplineTargetTracker  # Target tracking
└── SplineShapeKeeper    # Shape preservation

Movement/
├── SplineMovement       # Movement behavior base
└── ConstantSpeedMovement # Constant speed implementation

Editor/
├── SplineManagerEditor  # Spline manager inspector
├── SplineCursorEditor   # Cursor inspector
└── [Other Editors]      # Additional custom editors
```

## Advanced Features

### Custom Movement Behaviors
Extend `SplineMovement` to create custom movement patterns:

```csharp
public class CustomMovement : SplineMovement
{
    protected override void UpdateMovement()
    {
        // Implement custom movement logic
    }
}
```

### Tracking Modes
- **Direct**: Instant position tracking
- **Smooth**: Interpolated tracking with adjustable speed
- **Spring**: Physics-based elastic tracking
- **Limited**: Constrained within maximum distance

### Shape Preservation Modes
- **Rigid**: Maintains fixed relative positions
- **Elastic**: Allows controlled deformation
- **Absolute**: Preserves absolute offset sizes
- **ElasticBend**: Compensates compression with increased bending

## Examples

### Creating a Bezier Curve Path
```csharp
// Setup spline
GameObject splineObj = new GameObject("Spline");
SplineManager manager = splineObj.AddComponent<SplineManager>();
manager.splineType = SplineType.BezierSpline;

// Add control points
for (int i = 0; i < 4; i++)
{
    GameObject cp = new GameObject($"ControlPoint_{i}");
    cp.transform.parent = splineObj.transform;
    cp.transform.position = new Vector3(i * 2, 0, 0);
    cp.AddComponent<SplineControlPoint>();
}
```

### Following a Spline
```csharp
// Add cursor to follow the spline
GameObject follower = new GameObject("Follower");
follower.transform.parent = splineObj.transform;
SplineCursor cursor = follower.AddComponent<SplineCursor>();

// Animate along curve
float t = 0;
void Update()
{
    t += Time.deltaTime * 0.2f; // 20% per second
    cursor.Position = Mathf.PingPong(t, 1f);
}
```

## Requirements

- Unity 2021.3 or higher
- Universal Render Pipeline (URP) recommended

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

- Original implementation from **Nine Sols** by Red Candle Games
- Reimplemented with **Claude Code** by Anthropic
- Special thanks to the Nine Sols development team

## Support

For issues, questions, or contributions, please visit:
- [GitHub Issues](https://github.com/RED-CANDLE-GAMES-CO-LTD/CurveMaster/issues)
- [Red Candle Games](https://www.redcandlegames.com)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request