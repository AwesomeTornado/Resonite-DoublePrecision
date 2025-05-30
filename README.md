# DoublePrecision

This is a mod for the game Resonite which circumvents floating point errors. The mod intercepts the FrooxEngine-Unity connectors, and ensures that the Unity rendering scene is centered with (0,0,0) as the camera location, while the FrooxEngine coordinate system remains untouched.
Ideally, this allows for no floating point rendering errors, while at the same time avoiding any sync problems that would occur from messing with the FrooxEngine coordinates.