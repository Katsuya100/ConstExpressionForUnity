using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public class ConstExpressionForUnityPerformanceTest
    {
        [Test]
        [Performance]
        public void CalcPrime_ConstExpression()
        {
            Measure.Method(() =>
            {
                TestFunctions.FindLargestPrime(1000);
            })
            .WarmupCount(1)
            .IterationsPerMeasurement(10000)
            .MeasurementCount(20)
            .Run();
        }

        [Test]
        [Performance]
        public void CalcPrime_Raw()
        {
            Measure.Method(() =>
            {
                TestFunctions.FindLargestPrimeRaw(1000);
            })
            .WarmupCount(1)
            .IterationsPerMeasurement(10000)
            .MeasurementCount(20)
            .Run();
        }
    }
}
