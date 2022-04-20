# ColorRamp
Create Color Ramps for use with shaders or scripts.

Use `Assets->Create->Color Ramp` to create a new ColorRamp asset. 

A ColorRamp asset simply creates and updates a Texture2D as a sub asset.

Controls are available to adjust each gradient, add new gradients to the stack, reorder the gradients, and tweak the vertical transition between gradients.
`Gradient Type` currently supports `Horizontal`, `Vertical`, and `Radial`.
<div><img src="https://i.imgur.com/XUkKjof.png" width="400" /></div>

Ramps default to Clamp and Bilinear. As these are Texture2D sub assets, you can select them in the project browser to adjust those properties.
<div><img src="https://i.imgur.com/Fdq4neR.png" /></div>
<div><img src="https://i.imgur.com/Gh0JoOd.png" width="400" /></div>

A MaterialPropertyDrawer is provided for use with hand-written shaders (Shader Graph does not yet support custom property drawers).
<div><img src="https://i.imgur.com/BGBiZxV.png" width="600" /></div>
<div><img src="https://i.imgur.com/x9Ddxnw.png" width="600" /></div>

## API
In C# you can `.Evaluate` a ramp.

This samples the underlying texture with GetPixelBilinear, so it's not something you want to be doing every frame.

eg.

```
public Color Evaluate(float u)
ramp.Evaluate(0.5f); // the middle of the ramp
```
```
public Color Evaluate(Vector2 uv)
public Color Evaluate(float u, float v)
ramp.Evaluate(0.1f, 0.6f); // sample a 2D ramp with u and v coordinates (normalised 0-1)
```
