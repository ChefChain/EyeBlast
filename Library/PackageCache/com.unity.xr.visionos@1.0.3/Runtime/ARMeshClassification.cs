using System.ComponentModel;
// ReSharper disable InconsistentNaming

namespace UnityEngine.XR.VisionOS
{
    /// <summary>
    /// A value describing the classification of a mesh face.
    /// </summary>
    public enum ARMeshClassification : byte
    {
        /// <summary>
        /// The face type of the mesh is unknown.
        /// </summary>
        [Description("None")]
        None = 0,

        /// <summary>
        /// The face type of the mesh is wall.
        /// </summary>
        [Description("Wall")]
        Wall,

        /// <summary>
        /// The face type of the mesh is floor.
        /// </summary>
        [Description("Floor")]
        Floor,

        /// <summary>
        /// The face type of the mesh is ceiling.
        /// </summary>
        [Description("Ceiling")]
        Ceiling,

        /// <summary>
        /// The face type of the mesh is table.
        /// </summary>
        [Description("Table")]
        Table,

        /// <summary>
        /// The face type of the mesh is seat.
        /// </summary>
        [Description("Seat")]
        Seat,

        /// <summary>
        /// The face type of the mesh is window.
        /// </summary>
        [Description("Window")]
        Window,

        /// <summary>
        /// The face type of the mesh is door.
        /// </summary>
        [Description("Door")]
        Door,

        /// <summary>
        /// The face type of the mesh is a wall decoration.
        /// </summary>
        [Description("WallDecoration")]
        WallDecoration,

        /// <summary>
        /// The face type of the mesh is blinds.
        /// </summary>
        [Description("Blinds")]
        Blinds,

        /// <summary>
        /// The face type of the mesh is a fireplace.
        /// </summary>
        [Description("Fireplace")]
        Fireplace,

        /// <summary>
        /// The face type of the mesh is stairs.
        /// </summary>
        [Description("Stairs")]
        Stairs,

        /// <summary>
        /// The face type of the mesh is a bed.
        /// </summary>
        [Description("Bed")]
        Bed,

        /// <summary>
        /// The face type of the mesh is a counter.
        /// </summary>
        [Description("Counter")]
        Counter,

        /// <summary>
        /// The face type of the mesh is a cabinet.
        /// </summary>
        [Description("Cabinet")]
        Cabinet,

        /// <summary>
        /// The face type of the mesh is a home appliance.
        /// </summary>
        [Description("HomeAppliance")]
        HomeAppliance,

        /// <summary>
        /// The face type of the mesh is a door frame.
        /// </summary>
        [Description("DoorFrame")]
        DoorFrame,

        /// <summary>
        /// The face type of the mesh is a TV.
        /// </summary>
        [Description("TV")]
        TV,

        /// <summary>
        /// The face type of the mesh is a whiteboard.
        /// </summary>
        [Description("Whiteboard")]
        Whiteboard,

        /// <summary>
        /// The face type of the mesh is a plant.
        /// </summary>
        [Description("Plant")]
        Plant
    }
}
