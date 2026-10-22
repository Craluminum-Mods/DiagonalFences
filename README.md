
# DiagonalFences
**FOR REFERENCE**
Vanilla fence heights: 1.0625 for selection, 1.3125 for collision.

## Adding support for a fence
Fences connect diagonally through the `DiagonalFence` block behavior. The fence block itself stays the vanilla one: the mod only draws an extra arm towards each diagonal neighbour and adds matching selection and collision boxes. Supporting a new fence takes a JSON patch and an arm model, no code.

### An Example?
If you want a ready example for each of the fences supported you can view them in the folder `assets/diagonalfences/patches/` for the patch .json and `assets/diagonalfences/shapes/` for the models

### What the fence needs
- Its class is `BlockFence` or a subclass that still calls the original `OnJsonTesselation` and `GetSelectionBoxes`. `BlockFenceStackAware` (drystone and coral fences) replaces `OnJsonTesselation`, so those aren't supported yet.

- Its `type` variant uses vanilla's naming for connections: `empty`, `n`, `ew`, `nesw`... The mod reads those letters to know which sides are connected.

  

### The patch

  

Add the behavior to the fence's blocktype, for example in `assets/<yourmod>/patches/`:

  

```json

{

"op": "add",

"path": "/behaviors/-",

"value": {

"name": "DiagonalFence",

"properties": {

"armShape": "yourmod:block/fence/myfence/diagonal-arm"

}

},

"file": "yourmod:blocktypes/myfence.json",

"dependsOn": [{ "modid": "diagonalfences" }]

}

```

  

`dependsOn` keeps your patch from adding a behavior that doesn't exist when this mod isn't installed. Two fences connect diagonally as long as both have the behavior, even if they are different kinds.

  

| Property | Required | What it is |

|---|---|---|

| `armShape` | yes | The arm model. A single shape, or a list of shapes: each fence then picks one from its position, like vanilla's random shape alternates. |

| `snowArmShape` | no | Same, used when the fence's `cover` variant is `snow`. Meant for snowy blocktypes that `inheritFrom` the snow-free one and so share its behavior (the rough-hewn fence). A snowy blocktype with its own `behaviors` list gets its own patch entry with its own `armShape` instead (the wooden fence). |

| `cardinalOvershootShape` | no | For fences whose rails cross at every joint (bamboo). Rails continuing a west connection past the centre; added to dead ends that also connect diagonally, since vanilla's dead-end shapes stop their rails at the centre. |

  

Shape paths are written like vanilla's shape `base` paths: with the domain, without `shapes/` and `.json`.

  

### The arm model

  

One arm, the piece between the fence's centre and the block corner. The mod turns it for the other three corners.

  

-  **Point it north-east.** Converting a vanilla shape that points west (the one-sided `w` shape)? Turn its root element by 225° on Y around the block centre `[8, 0, 8]`.

-  **Reach the block corner.** From the centre to the corner is 11.32, use it so two neighbouring arms overlap a hair instead of leaving a gap.

-  **Leave out the centre post.** The vanilla fence already draws it in every connection shape.

-  **Mind the post.** Turned 45°, the arm meets a flat face of the post instead of its edge. Pieces that run into the post's middle can poke out of its sides: stop them just inside its face. Snow on top of the arm should stop short of the post (if it goes inside the post it shows, and then debugging it becomes a nightmare)

-  **Use the fence's own texture codes.** The arm is drawn with the fence block's textures, so every texture it uses must be one the fence block has, from its blocktype `textures` or from its vanilla shapes (bamboo uses `bamboo-side` and `bamboo-top`).

  

The models in `resources/assets/diagonalfences/shapes/block/wood/fence/` are working examples: `diagonal-arm` (wood), `bamboo/` (with `cardinal-overshoot`) and `roughfence/` (four alternates plus snow).

  

### Testing

  Every command is client side so you run it with a  `.` not with a  `/` , and the main command is  `.diagonalfences` or  `.df` for the shorthand

-  `.df debug` (or `.diagonalfences debug`) shows the diagonal connections of the fence you look at (in the top tooltip) and highlights the fences it connects to.

-  `.df snow on` covers the area around you in snow and keeps it there, `.df snow off` gives it back to the weather. Both need the privileges for vanilla's `/debug` commands.

- A missing or misspelled arm shape logs a warning naming the fence, `[diagonalfences] <block> has no usable armShape`, and that fence just won't connect diagonally.