---
uid: psl-vos-sorting-group
---
# visionOS Sorting Groups

The **VisionOS Sorting Group** component provides a way to use the visionOS platform's native sorting group capabilities. By placing renderers in a sorting group, you can get fine-grained control over which render is drawn first. While 2D components (such as SpriteRenderers and CanvasRenderers) have their own sorting mechanism, in some situations, it might help to override their sorting, or to sort non-2D renderers such as `MeshRenderers`.

A visionOS sorting group** can also be used to sort input on visionOS, in the same way renderers would be sorted. Game objects with colliders can be rearranged using a sorting group - colliders with higher order will capture input and block any lower order colliders from receiving input. Note that the physics of these colliders are not altered - the sorting group only affects the collider with respect to input resolution.

Any 2D renderers included in the `VisionOSSortingGroup` will have their default (sprite/canvas) sorting overridden. Each renderer can only belong to one sorting group at a time, and subsequent attempts to add a renderer to another sorting group will result in a warning.

To create a sorting group, add a **VisionOS Sorting Group** component to a GameObject in the scene. You can only add one **VisionOS Sorting Group** component per GameObject. You should avoid changing the component properties often -- changing these properties can be an expensive operation, especially if the **Apply To Descendants** option is enabled.

Sorting groups have no effect on how renderers are sorted against objects outside the group. They only affect how objects inside the group are sorted relative to each other.

The `VisionOSSortingGroup` component exposes the following properties:

| **Property** | **Description** |
| --- | --- |
| **DepthPass** | Controls when depth is drawn with respect to color. |
| **Renderers** | A list of structs that reference the renderer to be sorted, and the sort order. |

Each renderer struct consists of the following properties.

| **Property** | **Description** |
| --- | --- |
| **Order** | The order this renderer should be drawn, with respect to other renderers in the group. |
| **Renderer** | A reference to the renderer's game object.|
| **ApplyToDescendants** | When true, the sort order will be applied to all child renderers. It is important, if `ApplyToDescendants` is true, to be careful of nested sorting groups - any subsequent attempts to add a renderer that is already a member of a different sorting group will be ignored, and a warning will show. |
