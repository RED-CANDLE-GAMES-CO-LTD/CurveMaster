# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-08-21

### Added
- Initial release of CurveMaster package
- Multiple spline implementations: Bezier, B-Spline, Catmull-Rom, Cubic Spline
- SplineManager component for managing splines and control points
- SplineControlPoint component for individual control points
- SplineCursor component for following splines
- SplineTargetTracker for dynamic target tracking
- SplineShapeKeeper for maintaining curve shape
- Visual Scene editing with handles and gizmos
- Comprehensive editor tools and inspectors
- Sample scenes and prefabs
- Support for Unity 2021.3+

### Features
- Real-time spline editing in Scene view
- Multiple tracking modes (Direct, Smooth, Spring, Limited)
- Shape preservation modes (Rigid, Elastic, Absolute, ElasticBend)
- Performance optimized with caching system
- Proper transform hierarchy support
- Instant snap on enable option to prevent jitter
- Bilingual support (English/Chinese) in code comments

### Technical
- Clean interface-based architecture
- Modular component system
- Editor customization for all components
- Unity Package Manager compatible structure