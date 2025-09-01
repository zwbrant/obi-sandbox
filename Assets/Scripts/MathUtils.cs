using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Pyrenees.Scripts;
using UnityEngine;

public static class MathUtils
{
    public const float DecibelReferenceDistance = 1f;
    public const float HalfPi = 1.5707963267948966192313216916398f;

    // In kilograms per cubic meter
    public const float WaterDensity = 1000f;
    public const float AirDensity = 1.225f;
    // m/s^2
    public const float GravitationalAcceleration = 9.81f;

    // Use this to magnify the effect of distance on sound volume. Larger numbers mean less attenuation.
    // 20 is the realistic 1:1 scale number; it would result a loss of 20db over 10m
    private const float VolumeDistanceFalloffFactor = 14f;
    
    public const float ScalarEpsilon = 1e-4f;
    public const float VectorEpsilon = 1e-6f;
    // public const float ZeroCheckThreshold = 1e-6f;
    

    /// <summary>
    /// Exponential decay form of EMA. Old values become exponentially less impactful to new value. 
    /// </summary>
    /// <param name="smoothedValue">Current smooth value</param>
    /// <param name="inputValue">Raw target</param>
    /// <param name="updateTimeDelta">Time since last update</param>
    /// <param name="timeConstant">Seconds it takes to reach ~63% of a change</param>
    /// <returns></returns>
    public static float ExponentialSmooth(this float smoothedValue, float inputValue, float timeConstant, float updateTimeDelta)
    {
        float alpha = 1f - Mathf.Exp(-updateTimeDelta / timeConstant);
        // Basically equivalent to lerping where "alpha" is t
        return alpha * inputValue + (1f - alpha) * smoothedValue;
    }

    public static Vector3 ExponentialSmooth(this Vector3 smoothedValue, Vector3 inputValue, float timeConstant, float updateTimeDelta)
    {
        float alpha = 1f - Mathf.Exp(-updateTimeDelta / timeConstant);
        return Vector3.LerpUnclamped(smoothedValue, inputValue, alpha);
    }
    
    public static int FloorToInt(float f)
    {
        return (int)Math.Floor(f);
    }

    public static int Clamp(int n, int min, int max)
    {
        return n < min ? min : (n > max ? max : n);
    }

    public static Vector3 ClampToNonReverse(Vector3 referenceVector, Vector3 clampedVector) 
    {
        // Check if the result goes backwards
        if (Vector3.Dot(referenceVector, clampedVector) < 0)
        {
            return Vector3.zero; // Clamp to zero if reversing
        }
    
        // Otherwise, return the difference
        return clampedVector;
    }
    
    public static Vector3[] GetSphericalRays(int directionsCount, int fov, float vectorLength = 1f)
    {
        var fovMultiplier = (float)fov / 360f;

        var directions = new Vector3[(int)(directionsCount * fovMultiplier)];

        // Debug.Log($"Creating {directions.Length} vision directions {fov}");

        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < directions.Length; i++)
        {
            float t = (float)i / directionsCount;
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);
            directions[i] = new Vector3(x, y, z) * vectorLength;
        }

        return directions;
    }

    public static float GetDecibelsAtPosition(float startDecibels, float pitch, Vector3 startPosition, Vector3 targetPosition)
    {
        // Lp(R2) = Lp(R1) - 20·Log10(R2/R1)
        //
        // Where:
        // Lp(R1) = Known sound pressure level at the first location (typically measured data or equipment vendor data)
        // Lp(R2) = Unknown sound pressure level at the second location Location
        //     R1 = Distance from the noise source to location of known sound pressure level
        //     R2 = Distance from noise source to the second location

        float distance = Vector3.Distance(startPosition, targetPosition);
        float decibelAttenuation = -(VolumeDistanceFalloffFactor * Mathf.Log10(DecibelReferenceDistance / distance)) + WaterSoundAttenuationCoefficient * (pitch / 1000f) * distance;
        var decibelsAtPosition = startDecibels - decibelAttenuation;

        return Mathf.Max(0f, decibelsAtPosition);
    }

    private const float WaterSoundAttenuationCoefficient = .08f;


    public static float GetDistanceForDecibelTarget(float startDecibels, float targetDecibels)
    {
        var distanceRatio = Mathf.Pow(10f, (startDecibels - targetDecibels) / 20f);

        var targetDistance = distanceRatio * DecibelReferenceDistance;

        return targetDistance;
    }

    public static float GetTimeToStop(float currSpeed, float decelerationRate)
    {
        return -currSpeed / decelerationRate;
    }

    public static float GetLazyStoppingDistance(float currSpeed)
    {
        return Mathf.Pow(currSpeed, 2f) * .04f;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a">Length of side A</param>
    /// <param name="b">Length of side B</param>
    /// <param name="c">Length of side C</param>
    /// <returns></returns>
    public static float GetAreaOfTriangle(float a, float b, float c)
    {
        float s = (a + b + c) * .5f;

        return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
    }

    public static float GetAreaOfTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        return GetAreaOfTriangle(Vector3.Distance(a, b), Vector3.Distance(b, c), Vector3.Distance(c, a));
    }

    /// <param name="force">In Newtons</param>
    /// <param name="mass">In Kilograms</param>
    /// <returns></returns>
    public static float GetAcceleration(float netForce, float mass)
    {
        // F = m * a
        // a = F / m
        // Fnet = F - dragForce

        return netForce / mass;
    }

    public static Vector3 GetAcceleration(Vector3 netForce, float mass)
    {
        // F = m * a
        // a = F / m
        // Fnet = F - dragForce

        return netForce / mass;
    }

    // circle area = pi * r^2
    public static float GetDistanceMoved(float initialVelocity, float acceleration, float timeInterval)
    {
        // s = v0 * t + 0.5 * a * t^2
        return initialVelocity * timeInterval + 0.5f * acceleration * (timeInterval * timeInterval);
    }


    public static void ApplyForce(float mass, Vector3 initialVelocity, Vector3 force, float timeInterval, out Vector3 finalVelocity, out Vector3 positionChange)
    {
        var accelVector = GetAcceleration(force, mass);

        var velocityChange = accelVector * timeInterval;

        var velocityFinal = initialVelocity + velocityChange;

        var velocityAvg = (initialVelocity + velocityFinal) / 2f;

        positionChange = velocityAvg * timeInterval;
        finalVelocity = velocityFinal;
    }

    public static float GetMomentOfInertiaForSphere(float mass, float radius)
    {
        // I = 2/5 * M * R^2
        return (2f / 5f) * mass * (radius * radius);
    }


    public static float StandardDeviation(this IEnumerable<float> values)
    {
        float avg = values.Average();
        return (float)Math.Sqrt(values.Average(v => (v - avg) * (v - avg)));
    }

    public static float StandardDeviation(float weightedMean, float weightSum, IEnumerable<(float, float)> valuesAndWeights)
    {
        var weightedVariance = 0f;
        foreach (var valuesAndWeight in valuesAndWeights)
        {
            weightedVariance += valuesAndWeight.Item2 * ((valuesAndWeight.Item1 - weightedMean) * (valuesAndWeight.Item1 * weightedMean));
        }

        weightedVariance /= weightSum;
        return (float)Math.Sqrt(weightedVariance);
    }

    public static float GetSphereVolume(float radius)
    {
        return 1.3333333333333333333333333333333f * Mathf.PI * Mathf.Pow(radius, 3f);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="capHeight">Height of sphere section to measure. Clamped between 0 and sphere diameter. </param>
    /// <returns></returns>
    public static float GetVolumeOfSphereCap(float radius, float capHeight)
    {
        capHeight = Mathf.Clamp(capHeight, 0f, radius * 2f);
        return Mathf.PI * capHeight * capHeight * 0.3333333333333333f * (3f * radius - capHeight);
    }
    
    public static float GetSurfaceAreaOfSphereCap(float radius, float capHeight)
    {
        // S_(cap)	=	2piRh
        capHeight = Mathf.Clamp(capHeight, 0f, radius * 2f);
        return 2f * Mathf.PI * radius * capHeight;
    }
    
    public static float GetBoxVolume(Vector3 size)
    {
        return size.x * size.y * size.z;
    }

    public static float GetCylinderVolume(float radius, float height)
    {
        // πr^2h
        return Mathf.PI * Mathf.Pow(radius, 2f) * height;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The position of the projected point</returns>
    public static Vector3 ProjectPointOnLine(Vector3 lineA, Vector3 lineB, Vector3 point)
    {
        var aToB = lineB - lineA;
        var aToPoint = point - lineA;
        var aToBLengthSquared = Vector3.Dot(aToB, aToB);

        var aToD = (Vector3.Dot(aToPoint, aToB) / Vector3.Dot(aToB, aToB)) * aToB;

        if (Vector3.Dot(aToD, aToB) < 0)
        {
            return lineA;
        }

        if (Vector3.Dot(aToD, aToD) > aToBLengthSquared)
        {
            return lineB;
        }

        return lineA + aToD;
    }

    public static void FindClosestPointsOnLines(Vector3 A, Vector3 B, Vector3 C, Vector3 D, out Vector3 closestOnAB, out Vector3 closestOnCD, bool clampOnLines = true)
    {
        Vector3 u = B - A; // Direction vector of line AB
        Vector3 v = D - C; // Direction vector of line CD
        Vector3 w = A - C;
        float a = Vector3.Dot(u, u); // Squared magnitude of u
        float b = Vector3.Dot(u, v);
        float c = Vector3.Dot(v, v); // Squared magnitude of v
        float d = Vector3.Dot(u, w);
        float e = Vector3.Dot(v, w);
        float denominator = a * c - b * b; // Denominator for the equations

        float s, t;
        if (denominator < 1e-6) // Lines are almost parallel
        {
            t = 0;
            s = (b > c ? d / b : e / c); // Use the larger denominator
        }
        else
        {
            t = (b * e - c * d) / denominator;
            s = (a * e - b * d) / denominator;
        }

        // print($"t:{t}, s:{s}");

        if (clampOnLines)
        {
            t = Mathf.Clamp01(t);
            s = Mathf.Clamp01(s);
        }

        closestOnAB = A + t * u;
        closestOnCD = C + s * v;
    }
    
    public static int SecondsToMilliseconds(float seconds)
    {
        // Multiply by 1000 to convert to milliseconds
        float millisecondsFloat = seconds * 1000f;

        // Convert the float result to an int
        int milliseconds = (int)millisecondsFloat;

        return milliseconds;
    }

    public static Vector3 MultiplyVectors(params Vector3[] vectors)
    {
        if (vectors.Length is 0)
            return Vector3.zero;
        
        var product = vectors[0];
        for (int i = 1; i < vectors.Length; i++)
        {
            product.x *= vectors[i].x;
            product.y *= vectors[i].y;
            product.z *= vectors[i].z;
        }

        return product;
    }
    
    // StepX will be 0, 1, -1 depending on the ray's x direction.
    public static sbyte GetSign(float value) {
        return value switch
        {
            > 0f => 1,
            < 0f => -1,
            _ => 0
        };
    }

    // public static Vector3 AsVector(this VectorAxis axis)
    // {
    //     return axis switch
    //     {
    //         VectorAxis.X => Vector3.right,
    //         VectorAxis.Y => Vector3.up,
    //         VectorAxis.Z => Vector3.forward,
    //         _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
    //     };
    // }
    
    

    #region Lift & Drag Physics

    public static float GetAngleOfAttackRadians(Vector3 fluidVelocity, Vector3 surfaceNormal)
    {
        var projectedVelocity = Vector3.ProjectOnPlane(fluidVelocity, surfaceNormal).normalized;

        float angle = Vector3.Angle(fluidVelocity, projectedVelocity);
		
        return angle * Mathf.Deg2Rad;
    }
	
    public static float GetLiftMagnitude(Vector3 fluidVelocity, float surfaceArea, float liftCoefficient, float fluidDensity)
    {
        // L = 1/2 * ρ * v² * A * Cl
        float force = .5f * fluidDensity * fluidVelocity.sqrMagnitude * surfaceArea * liftCoefficient;

        return force;
    }

    public static Vector3 GetLiftDirection(Vector3 fluidVelocity, Vector3 surfaceNormal)
    {
        return Vector3.Cross(Vector3.Cross(fluidVelocity, surfaceNormal), fluidVelocity).normalized;
    }

    public static float GetLiftCoefficient(float angleOfAttackRadians, float minCoefficient, float maxCoefficient)
    {
        return Mathf.Max(minCoefficient, Mathf.Sin(2f * angleOfAttackRadians)) * maxCoefficient;
    }

    #endregion
}