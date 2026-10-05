using System.Text;

namespace DragoAnt.Observer.Tests;

public sealed class DataPathTests
{
    [Fact]
    public void Default_IsEmpty()
    {
        var path = default(DataPath);

        path.Length.Should().Be(0);
        path.ToString().Should().BeEmpty();
        path.LastName.IsEmpty.Should().BeTrue();
        path.GetName(0).Should().BeNull();
        path.TryFormat(Span<char>.Empty, out var written).Should().BeTrue();
        written.Should().Be(0);
    }

    [Fact]
    public void NamesAndItems_RenderAndReadBack()
    {
        var input = "xx{\"lines\":"u8.ToArray();
        var path = new DataPath(input, 1, true);
        try
        {
            path.PushInputName(4, 5);
            path.PushItem(2);
            path.PushName("a.b"u8);
            path.PushName("it's"u8);

            path.Length.Should().Be(4);
            path.MaxLength.Should().Be(4);
            path.ToString().Should().Be("lines[2]['a.b']['it\\'s']");
            path.GetName(0).Should().Be("lines");
            path.GetNameFromEnd(1).Should().Be("a.b");
            path.IsItem(1).Should().BeTrue();
            path.TryGetItemIndex(1, out var item).Should().BeTrue();
            item.Should().Be(2);
            path.TryGetItemIndex(0, out item).Should().BeFalse();
            item.Should().Be(-1);
            path.GetName(1).Should().BeNull();
            Encoding.UTF8.GetString(path.LastName).Should().Be("it's");

            path.Pop();
            path.Pop();
            path.ToString().Should().Be("lines[2]");
            path.LastName.IsEmpty.Should().BeTrue();
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void ReservedName_IsDecodedInPlace()
    {
        var path = new DataPath(default, 1, true);
        try
        {
            var room = path.ReserveName(10);
            "card"u8.CopyTo(room);
            path.PushReservedName(4);
            path.PushName("number"u8);

            path.ToString().Should().Be("card.number");
        }
        finally
        {
            path.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(11)]
    [InlineData(64)]
    public void TryFormat_MatchesToString_OrReportsShortBuffer(int size)
    {
        var path = new DataPath(default, 1, true);
        try
        {
            path.PushName("order"u8);
            path.PushItem(12);
            path.PushName(""u8);
            path.PushName("пароль"u8);
            var text = path.ToString();
            var buffer = new char[size];

            var ok = path.TryFormat(buffer, out var written);

            ok.Should().Be(size >= text.Length);
            if (ok)
            {
                new string(buffer, 0, written).Should().Be(text);
            }
            else
            {
                written.Should().Be(0);
            }
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void Text_OfAnEscapedName()
    {
        var path = new DataPath(default, 1, true);
        try
        {
            path.PushName("o'b.x"u8);
            var buffer = new char[32];

            path.TryFormat(buffer, out var written).Should().BeTrue();

            new string(buffer, 0, written).Should().Be(path.ToString()).And.Be("['o\\'b.x']");
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void Grows_PastItsCapacity()
    {
        var path = new DataPath(default, 1, true);
        try
        {
            for (var i = 0; i < 40; i++)
            {
                path.PushName(Encoding.UTF8.GetBytes(new string('n', 300)));
            }

            path.Length.Should().Be(40);
            path.GetName(39).Should().HaveLength(300);
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void PushInputName_OutsideTheInput_Throws()
    {
        var thrown = false;
        var path = new DataPath("ab"u8, 1, true);
        try
        {
            path.PushInputName(1, 5);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }
        finally
        {
            path.Dispose();
        }

        thrown.Should().BeTrue();
    }
}
