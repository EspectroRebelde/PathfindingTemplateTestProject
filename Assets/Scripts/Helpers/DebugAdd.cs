using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
 
/* Following the tutorials on https://dev-tut.com/ 
 * for the drawing of the different shapes.
 * Recreated and added descriptions and documentation by Daniel Muñoz
 * Last edited: 09/04/2022
 */

/// <summary>
/// Multiple extra functions for easier visual debugging
/// </summary>
/// <remarks>From the tutorials at https://dev-tut.com/ </remarks>
internal abstract class Debug : UnityEngine.Debug
{
    /// <summary>
    /// Draws a circle at a specified position.
    /// </summary>
    /// <param name="position">Origin of the circle</param>
    /// <param name="rotation">Rotation of the circle</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="color">Color to draw the lines as</param>
    /// <param name="segments">Segments used to draw the lines</param>
    public static void DrawCircle(Vector3 position, Quaternion rotation, float radius, Color color, int segments)
    {
        // If either radius or number of segments are less or equal to 0, skip drawing
        if (radius <= 0.0f || segments <= 0) return;

        // Single segment of the circle covers (360 / number of segments) degrees
        float angleStep = (360.0f / segments);
 
        // Result is multiplied by Mathf.Deg2Rad constant which transforms degrees to radians
        // which are required by Unity's Mathf class trigonometry methods
        angleStep *= Mathf.Deg2Rad;
 
        // lineStart and lineEnd variables are declared outside of the following for loop
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;
 
        for (int i = 0; i < segments; i++) {
            // Line start is defined as starting angle of the current segment (i)
            lineStart.x = Mathf.Cos(angleStep * i);
            lineStart.y = Mathf.Sin(angleStep * i);
            lineStart.z = 0.0f;
 
            // Line end is defined by the angle of the next segment (i+1)
            lineEnd.x = Mathf.Cos(angleStep * (i + 1));
            lineEnd.y = Mathf.Sin(angleStep * (i + 1));
            lineEnd.z = 0.0f;
 
            // Results are multiplied so they match the desired radius
            lineStart *= radius;
            lineEnd *= radius;
 
            // Results are multiplied by the rotation quaternion to rotate them 
            // since this operation is not commutative, result needs to be
            // reassigned, instead of using multiplication assignment operator (*=)
            lineStart = rotation * lineStart;
            lineEnd = rotation * lineEnd;
 
            // Results are offset by the desired position/origin 
            lineStart += position;
            lineEnd += position;
 
            // Points are connected using DrawLine method and using the passed color
            DrawLine(lineStart, lineEnd, color);
        }
    }
    
    /// <summary>
    /// Draws a wireframe sphere at the specified position.
    /// </summary>
    /// <param name="position">Origin of the sphere</param>
    /// <param name="orientation">Rotation of the sphere (to match straight up and down with poles)</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="color">Color to draw the lines as</param>
    /// <param name="segments">Number of segments to draw the line as (min 2)</param>
    public static void DrawSphere(Vector3 position, Quaternion orientation, float radius, Color color, 
        int rays, int segments, float alpha = 1.0f, bool useMultipleColors = true)
    {
        Vector3 start = orientation * new Vector3(0, radius, 0);
        for(float i = 0; i < 360.0f; i += 360f/rays)
        {
            //Rotate quaternion by i degrees
            Quaternion rotationQuaternion = Quaternion.AngleAxis(i, start);
            Color rayColor = useMultipleColors ? Color.HSVToRGB(i / 360.0f, 1.0f, 1.0f) : color;
            rayColor.a = alpha;
 
            DrawCircle(position, rotationQuaternion, radius, rayColor,Mathf.Clamp(segments, 2, 360));
        }
    }
    
    /// <summary>
    /// Draws rays around a point
    /// </summary>
    /// <param name="position">Origin of the rays</param>
    /// <param name="rotationalAxis">Axis of the rays</param>
    /// <param name="rays">Number of rays to fill the 360 degrees</param>
    public static void DrawRaysAround(Vector3 position, Vector3 rotationalAxis, int rays = 24)
    {
        for(float i = 0; i < 360.0f; i += 360f/rays)
        {
            var rotationQuaternion = Quaternion.Euler(rotationalAxis*i);
            var rayColor = Color.HSVToRGB(i/360.0f, 1.0f, 1.0f);
 
            Debug.DrawRay(position, position + (rotationQuaternion * Vector3.right), rayColor);
        }
    }
    
    /// <summary>
    /// Draws an arc 
    /// </summary>
    /// <param name="startAngle">Angle from center to starting point</param>
    /// <param name="endAngle">Angle from center to endign point</param>
    /// <param name="position">Center of the circle</param>
    /// <param name="orientation">Orientation of the circle</param>
    /// <param name="radius">Radius of the circle</param>
    /// <param name="color">Color to draw the lines as</param>
    /// <param name="drawChord">Draws a line joining the two points</param>
    /// <param name="drawSector">Draws a line from the point to the center</param>
    /// <param name="arcSegments">Segments used to draw the arc (use more for more curveness)</param>
    public static void DrawArc(float startAngle, float endAngle, Vector3 position, Quaternion orientation, float radius, 
    Color color, bool drawChord = false, bool drawSector = false, int arcSegments = 32)
    {
        float arcSpan = Mathf.DeltaAngle(startAngle, endAngle);
     
        // Since Mathf.DeltaAngle returns a signed angle of the shortest path between two angles, it 
        // is necessary to offset it by 360.0 degrees to get a positive value
        if (arcSpan <= 0)
        {
            arcSpan += 360.0f;
        }
     
        // angle step is calculated by dividing the arc span by number of approximation segments
        float angleStep = (arcSpan / arcSegments) * Mathf.Deg2Rad;
        float stepOffset = startAngle * Mathf.Deg2Rad;
     
        // stepStart, stepEnd, lineStart and lineEnd variables are declared outside of the following for loop
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;
     
        // arcStart and arcEnd need to be stored to be able to draw segment chord
        Vector3 arcStart = Vector3.zero;
        Vector3 arcEnd = Vector3.zero;
     
        // arcOrigin represents an origin of a circle which defines the arc
        Vector3 arcOrigin = position;
     
        for (int i = 0; i < arcSegments; i++)
        {
            // Calculate approximation segment start and end, and offset them by start angle
            float stepStart = angleStep * i + stepOffset;
            float stepEnd = angleStep * (i + 1) + stepOffset;
     
            lineStart.x = Mathf.Cos(stepStart);
            lineStart.y = Mathf.Sin(stepStart);
            lineStart.z = 0.0f;
     
            lineEnd.x = Mathf.Cos(stepEnd);
            lineEnd.y = Mathf.Sin(stepEnd);
            lineEnd.z = 0.0f;
     
            // Results are multiplied so they match the desired radius
            lineStart *= radius;
            lineEnd *= radius;
     
            // Results are multiplied by the orientation quaternion to rotate them 
            // since this operation is not commutative, result needs to be
            // reassigned, instead of using multiplication assignment operator (*=)
            lineStart = orientation * lineStart;
            lineEnd = orientation * lineEnd;
     
            // Results are offset by the desired position/origin 
            lineStart += position;
            lineEnd += position;
     
            // If this is the first iteration, set the chordStart
            if (i == 0)
            {
                arcStart = lineStart;
            }
     
            // If this is the last iteration, set the chordEnd
            if(i == arcSegments - 1)
            {
                arcEnd = lineEnd;
            }
     
            DrawLine(lineStart, lineEnd, color);
        }
     
        if (drawChord)
        {
            DrawLine(arcStart, arcEnd, color);
        }
        if (drawSector)
        {
            DrawLine(arcStart, arcOrigin, color);
            DrawLine(arcEnd, arcOrigin, color);
        }
    }
    
    /// <summary>
    /// Basic triangle drawing function
    /// </summary>
    /// <param name="pointA">First vertex</param>
    /// <param name="pointB">Second vertex</param>
    /// <param name="pointC">Thrid vertex</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Color color)
    {
        // Connect pointA and pointB
        Debug.DrawLine(pointA, pointB, color);
 
        // Connect pointB and pointC
        Debug.DrawLine(pointB, pointC, color);
 
        // Connect pointC and pointA
        Debug.DrawLine(pointC, pointA, color);
    }
    
    /// <summary>
    /// Basic triangle drawing function with orientation and offset applied
    /// </summary>
    /// <param name="pointA">First vertex</param>
    /// <param name="pointB">Second vertex</param>
    /// <param name="pointC">Third vertex</param>
    /// <param name="offset">Offset from center</param>
    /// <param name="orientation">Orientation of the triangle</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 offset, Quaternion orientation, Color color)
    {
        pointA = offset + orientation * pointA;
        pointB = offset + orientation * pointB;
        pointC = offset + orientation * pointC;
 
        DrawTriangle(pointA, pointB, pointC, color);
    }
    
    /// <summary>
    /// Isosceles triangle
    /// </summary>
    /// <param name="origin">Center of the triangle</param>
    /// <param name="orientation">Orientation of the triangle</param>
    /// <param name="baseLength">Lenght of the base</param>
    /// <param name="height">Height of the triangle</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawTriangle(Vector3 origin, Quaternion orientation, float baseLength, float height, Color color)
    {
        Vector3 pointA = Vector3.right * baseLength * 0.5f;
        Vector3 pointC = Vector3.left * baseLength * 0.5f;
        Vector3 pointB = Vector3.up * height;
 
        DrawTriangle(pointA, pointB, pointC, origin, orientation, color);
    }
    
    /// <summary>
    /// Equilateral triangle
    /// </summary>
    /// <param name="length">From center to vertex</param>
    /// <param name="center">Center of the triangle</param>
    /// <param name="orientation">Orientation of the triangle</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawTriangle(float length, Vector3 center, Quaternion orientation, Color color)
    {
        float radius = length / Mathf.Cos(30.0f * Mathf.Deg2Rad) * 0.5f;
        Vector3 pointA = new Vector3(Mathf.Cos(330.0f * Mathf.Deg2Rad), Mathf.Sin(330.0f * Mathf.Deg2Rad), 0.0f) * radius;
        Vector3 pointB = new Vector3(Mathf.Cos(90.0f * Mathf.Deg2Rad), Mathf.Sin(90.0f * Mathf.Deg2Rad), 0.0f) * radius;
        Vector3 pointC = new Vector3(Mathf.Cos(210.0f * Mathf.Deg2Rad), Mathf.Sin(210.0f * Mathf.Deg2Rad), 0.0f) * radius;
 
        DrawTriangle(pointA, pointB, pointC, center, orientation, color);
    }
    
    /// <summary>
    /// Basic quad drawing function
    /// </summary>
    /// <param name="pointA">First point</param>
    /// <param name="pointB">Second point</param>
    /// <param name="pointC">Thrid point</param>
    /// <param name="pointD">Forth point</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawQuad(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD, Color color)
    {
        // Draw lines between the points
        DrawLine(pointA, pointB, color);
        DrawLine(pointB, pointC, color);
        DrawLine(pointC, pointD, color);
        DrawLine(pointD, pointA, color);
    }
    
    /// <summary>
    /// Draw a rectangle defined by its position, orientation and extent
    /// </summary>
    /// <param name="position">Three dimensional start for the quad</param>
    /// <param name="orientation">Orientation of the quad</param>
    /// <param name="extent">Width and heigh of the quad</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawRectangle(Vector3 position, Quaternion orientation, Vector2 extent, Color color)
    {
        Vector3 rightOffset = Vector3.right * extent.x * 0.5f;
        Vector3 upOffset = Vector3.up * extent.y * 0.5f;
 
        Vector3 offsetA = orientation * (rightOffset + upOffset);
        Vector3 offsetB = orientation * (-rightOffset + upOffset);
        Vector3 offsetC = orientation * (-rightOffset - upOffset);
        Vector3 offsetD = orientation * (rightOffset - upOffset);
 
        DrawQuad(   position + offsetA,
                    position + offsetB,
                    position + offsetC,
                    position + offsetD, 
                    color);
    }
    
    /// <summary>
    /// Draw a rectangle defined by two points, origin and orientation
    /// </summary>
    /// <param name="point1">Width and height from origin</param>
    /// <param name="point2">Width and height from origin</param>
    /// <param name="origin">Origin point</param>
    /// <param name="orientation">Orientation of the quad</param>
    /// <param name="color">Color to draw the lines as</param>
    public static void DrawRectangle(Vector2 point1, Vector2 point2, Vector3 origin, Quaternion orientation, Color color)
    {
        // Calculate extent as a distance between point1 and point2
        float extentX = Mathf.Abs(point1.x - point2.x);
        float extentY = Mathf.Abs(point1.y - point2.y);
 
        // Calculate rotated axes
        Vector3 rotatedRight = orientation * Vector3.right;
        Vector3 rotatedUp = orientation * Vector3.up;
         
        // Calculate each rectangle point
        Vector3 pointA = origin + rotatedRight * point1.x + rotatedUp * point1.y;
        Vector3 pointB = pointA + rotatedRight * extentX;
        Vector3 pointC = pointB + rotatedUp * extentY;
        Vector3 pointD = pointA + rotatedUp * extentY;
 
        DrawQuad(pointA, pointB, pointC, pointD, color);
    }

    public static void DrawCircle(float3 position, quaternion aligmentQuaternion, float radius, Color color, int segments)
    {
        Vector3 pos = new Vector3(position.x, position.y, position.z);
        Quaternion or = new Quaternion(aligmentQuaternion.value.x, aligmentQuaternion.value.y, aligmentQuaternion.value.z, aligmentQuaternion.value.w);
        DrawCircle(pos, or, radius, color, segments);
    }

    public static void DrawSphere(float3 position, quaternion orientation, float radius, Color color,
        int rays, int segments, float alpha = 1.0f, bool useMultipleColors = true)
    {
        Vector3 pos = new Vector3(position.x, position.y, position.z);
        Quaternion or = new Quaternion(orientation.value.x, orientation.value.y, orientation.value.z, orientation.value.w);
        DrawSphere(pos, or, radius, color, rays, segments, alpha, useMultipleColors);
    }
    
    /// <summary>
    /// Extended line drawing function with multiple new parameters
    /// </summary>
    /// <returns></returns>
    public static void DrawLineExtended(Vector3 start, Vector3 end, Color color, float thickness = 1.0f, float duration = 0.0f, bool depthTest = true)
    {
        // Draw the line
        Debug.DrawLine(start, end, color, duration, depthTest);

        // Calculate the rotation
        Quaternion rotation = Quaternion.LookRotation(end - start);
 
        // Calculate the scale
        Vector3 scale = new Vector3(thickness, (end - start).magnitude, thickness);
 
        // Draw the line
        Mesh lineMesh = new Mesh
        {
            vertices = new[] { start, end },
            colors = new[] { color, color },
        };
        lineMesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);
        
        Material lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        Graphics.DrawMesh(lineMesh, Matrix4x4.TRS(start, rotation, scale), lineMaterial, 0);
        
    }
    
    //Only compile in editor pragma
#if UNITY_EDITOR
    public static void ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
#endif
}