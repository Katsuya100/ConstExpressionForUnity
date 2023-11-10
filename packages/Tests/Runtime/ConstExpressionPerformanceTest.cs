using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public class ConstExpressionPerformanceTest
    {
        [Test]
        [Performance]
        public void CalcPrime_ConstExpression()
        {
            Measure.Method(() =>
            {
                ConstExpressionTestFunctions.FindLargestPrime(1000);
            })
            .WarmupCount(1)
            .IterationsPerMeasurement(10000)
            .MeasurementCount(20)
            .Run();
        }

        [Test]
        [Performance]
        public void CalcPrime_StaticExpression()
        {
            Measure.Method(() =>
            {
                StaticExpressionTestFunctions.FindLargestPrime(1000);
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
                ConstExpressionTestFunctions.FindLargestPrimeRaw(1000);
            })
            .WarmupCount(1)
            .IterationsPerMeasurement(10000)
            .MeasurementCount(20)
            .Run();
        }
    }
}
