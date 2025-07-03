```

BenchmarkDotNet v0.15.2, Linux EndeavourOS
Intel Core i7-4712MQ CPU 2.30GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100-preview.5.25277.114
  [Host]     : .NET 10.0.0 (10.0.25.27814), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.0 (10.0.25.27814), X64 RyuJIT AVX2


```
| Method              | Mean     | Error   | StdDev  |
|-------------------- |---------:|--------:|--------:|
| GenericsAdd         | 134.4 ns | 0.44 ns | 0.39 ns |
| NonGenericsAdd      | 134.6 ns | 0.40 ns | 0.38 ns |
| GenericsMultiply    | 440.6 ns | 1.35 ns | 1.13 ns |
| NonGenericsMultiply | 451.2 ns | 6.22 ns | 5.20 ns |
