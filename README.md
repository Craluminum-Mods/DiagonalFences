
# DiagonalFences
**FOR REFERENCE**
Vanilla fence heights: 1.0625 for selection, 1.3125 for collision.

## Adding support for a fence
Fences connect diagonally through the `DiagonalFence` block behavior. The fence block itself stays the vanilla one: the mod only draws an extra arm towards each diagonal neighbour and adds matching selection and collision boxes. Supporting a new fence takes a JSON patch and an arm model, no code.

### Supported fences
| Fence | Notes |
|---|---|
| Wooden fence | Plain and snowy, one arm each |
| Bamboo fence | Rails cross at every joint like vanilla's, see `cardinalOvershootShape` |
| Rough-hewn fence | Four random arm variants, plus three snowy ones |
| Wattle fence | Three random arm variants, no snowy version in vanilla |
| Drystone fence | Plain and snowy, with stacked arms for walls with another fence on top |
| Coral fence | Same as drystone |

The iron fence isn't supported: it can't be crafted, only taken from the creative inventory.

### An Example?
If you want a ready example for each of the fences supported you can view them in the folder `assets/diagonalfences/patches/` for the patch .json and `assets/diagonalfences/shapes/` for the models

### What the fence needs
- Its class is `BlockFence` or a subclass. A subclass that replaces `OnJsonTesselation` or `GetSelectionBoxes` without calling the original also needs its own Harmony patch, like the one for `BlockFenceStackAware` (drystone and coral fences) in `src/Patches/BlockFencePatches.cs`.

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
| `stackedArmShape` | no | Same, used when another fence sits directly on top. Stone walls drop their capstones then and continue upwards, so their arms have to as well (drystone, coral). |
| `cardinalOvershootShape` | no | For fences whose rails cross at every joint (bamboo). Rails continuing a west connection past the centre; added to dead ends that also connect diagonally, since vanilla's dead-end shapes stop their rails at the centre. |
| `diagonalPostRotation` | no | Degrees to turn the post of a fence that connects only diagonally, the arms stay as they are. A post set diagonally to straight rails looks square next to diagonal ones: `45` gives it the same look again. Fences with a straight connection and lone posts aren't touched, and the selection and collision boxes stay square. |

  

Shape paths are written like vanilla's shape `base` paths: with the domain, without `shapes/` and `.json`.

  

### The arm model

  

One arm, the piece between the fence's centre and the block corner. The mod turns it for the other three corners.

  

-  **Point it north-east.** Converting a vanilla shape that points west (the one-sided `w` shape)? Turn its root element by 225° on Y around the block centre `[8, 0, 8]`.

-  **Count that turn on vertical posts.** A post inside the arm ends up turned by 225° plus its own `rotationY` in the world, and the mod adds multiples of 90° for the other corners. So its `rotationY` is the turn you want in the world minus 225°: a post that should stand as a 45° diamond like vanilla's bamboo and wattle posts gets `0`, not `45`.

-  **Reach the block corner.** From the centre to the corner is 11.32, use it so two neighbouring arms overlap a hair instead of leaving a gap. Thick walls are the exception: end them exactly at 11.314, overlapping wall sides lie in the same plane and flicker.

-  **Stretch woven shapes as a whole.** Moving the pieces of a woven shape one by one pulls it apart. Put them in one group element with `scaleX` instead, it lengthens everything inside around its `rotationOrigin` (wattle stretches by 1.414).

-  **Leave out the centre post.** The vanilla fence already draws it in every connection shape.

-  **Mind the post.** Turned 45°, the arm meets a flat face of the post instead of its edge. Pieces that run into the post's middle can poke out of its sides: stop them just inside its face. Snow on top of the arm should stop short of the post (if it goes inside the post it shows, and then debugging it becomes a nightmare)

-  **Keep stone capstones under the joint's.** On stone walls the joint is covered by vanilla's centre pillar or straight wall, with their own capstones. The arm's capstones reaching into it are kept just below those (drystone 13.9, coral 15.5), they hide underneath instead of poking through at another height.

-  **Use the fence's own texture codes.** The arm is drawn with the fence block's textures (bamboo uses `bamboo-side` and `bamboo-top`). A code the fence block doesn't know falls back to the arm model's own `textures` list: a fence variant only knows the textures of its own shape, and vanilla's unconnected wattle fence has no `wattle` texture. Write those paths with the domain, `game:block/wattle`.

  

The models in `resources/assets/diagonalfences/shapes/block/` are working examples:
- `wood/fence/`: `diagonal-arm` (wood), `bamboo/` (with `cardinal-overshoot`), `roughfence/` (four alternates plus snow) and `wattle/` (stretched with `scaleX`)
- `stone/`: `drystonefence/` and `fencecoral/`, each with stacked and snowy arms

  

### Testing

  Every command is client side so you run it with a  `.` not with a  `/` , and the main command is  `.diagonalfences` or  `.df` for the shorthand

-  `.df debug` (or `.diagonalfences debug`) shows the diagonal connections of the fence you look at (in the top tooltip) and highlights the fences it connects to.

-  `.df snow on` covers the area around you in snow and keeps it there, `.df snow off` gives it back to the weather. Both need the privileges for vanilla's `/debug` commands.

- A missing or misspelled arm shape logs a warning naming the fence, `[diagonalfences] <block> has no usable armShape`, and that fence just won't connect diagonally.