
namespace OsmVisualizer.Data.Types
{
    public enum Direction
    {
        NONE,
        
        THROUGH,
        REVERSE,
        
        LEFT,
        LEFT_MERGE,
        LEFT_SLIGHT,
        LEFT_SHARP,
        
        RIGHT,
        RIGHT_SLIGHT,
        RIGHT_SHARP,
        RIGHT_MERGE
    }

    public static class DirectionExtension
    {
        public static Direction Simple(this Direction dir)
        {
            switch (dir)
            {
                case Direction.LEFT:
                case Direction.LEFT_SLIGHT:
                case Direction.LEFT_SHARP:
                    return Direction.LEFT;
                
                case Direction.RIGHT:
                case Direction.RIGHT_SLIGHT:
                case Direction.RIGHT_SHARP:
                    return Direction.RIGHT;
                
                default:
                    return dir;
            }
        }
        
        public static Direction[][] ToDirections(this string value)
        {
            var dirs = value.Split('|');
        
            var dirsArray = new Direction[dirs.Length][];
            
            for (var i = 0; i < dirs.Length; i++)
            {
                dirsArray[i] = dirs[i].ToLaneDirections();
            }

            return dirsArray;
        }
        

        public static Direction[] ToLaneDirections(this string value)
        {
            var d = value.Split(';');
            var dirs = new Direction[d.Length];

            for (var j = 0; j < d.Length; j++)
            {
                dirs[j] = d[j].ToDirection();
            }

            return dirs;
        }
        
        public static Direction ToDirection(this string value)
        {
            switch (value)
            {
                case "left":
                    return Direction.LEFT;
                
                case "slight_left":
                    return Direction.LEFT_SLIGHT;
                
                case "sharp_left":
                    return Direction.LEFT_SHARP;
                
                case "merge_to_left":
                    return Direction.LEFT_MERGE;
            
                case "right":
                    return Direction.RIGHT;
                
                case "slight_right":
                    return Direction.RIGHT_SLIGHT;
                
                case "sharp_right":
                    return Direction.RIGHT_SHARP;
                
                case "merge_to_right":
                    return Direction.RIGHT_MERGE;
            
                case "through":
                    return Direction.THROUGH;
            
                case "reverse":
                    return Direction.REVERSE;
                        
                // case "":
                // case "none":
                default:
                    return Direction.NONE;
            }
        }
    }

}