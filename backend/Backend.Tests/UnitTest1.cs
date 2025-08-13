namespace Backend.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // Arrange
        var expected = 6;
        var actual = 2 + 3;

        // Act & Assert
        Assert.Equal(expected, actual);
    }
}