//////////////////////////////////////////////////////////////////////
// STATIC SYNTAX FOR EXPRESSING OUTPUT CHECKS
//////////////////////////////////////////////////////////////////////

//public static class StringConstraints
//{
//	private string _outputText;

//	public static bool Contains(this string output)
//	{
//		return new StringCOnt();
//	}

//	public OutputCheck(string outputText)
//	{
//		_outputText = outputText;
//	}

//	public bool Contains(string content)
//	{
//		return output.Contains(content);
//	}
//}

public static OutputContainsCheck Contains(string expectedText, int atleast = 1, int exactly = -1)
    => new OutputContainsCheck(expectedText, atleast, exactly);

public static OutputDoesNotContain DoesNotContain(string text) => new OutputDoesNotContain(text);

// OutputCheck is used to check content of redirected package test output
public abstract class OutputCheck
{
	protected string _expectedText;
    protected int _atleast;
    protected int _exactly;

	public OutputCheck(string expectedText, int atleast = 1, int exactly = -1)
	{
		_expectedText = expectedText;
        _atleast = atleast;
        _exactly = exactly;
	}

    public bool Matches(IEnumerable<string> output) => Matches(string.Join("\r\n", output));

    public abstract bool Matches(string output);

	public string Message { get; protected set; }
}

public class OutputContainsCheck : OutputCheck
{
    public OutputContainsCheck(string expectedText, int atleast = 1, int exactly = -1) : base(expectedText, atleast, exactly) { }

    public override bool Matches(string output)
    {
        int found = 0;

        int index = output.IndexOf(_expectedText);
        int textLength = _expectedText.Length;
        int outputLength = output.Length;
        while (index >= 0 && index < output.Length - textLength)
        {
            ++found;
            index += textLength;
            index = output.IndexOf(_expectedText, index);
        }

        if (_atleast > 0 && found >= _atleast || _exactly > 0 && found == _exactly)
            return true;

        var sb = new StringBuilder("   Expected: ");
        if (_atleast > 0)
        {
            sb.Append($"at least {_atleast} ");
            sb.Append(_atleast == 1 ? "line " : "lines ");
            sb.Append($"containing \"{_expectedText}\" but found {found}");
        }
        else
        {
            sb.Append($"exactly {_exactly} ");
            sb.Append(_exactly == 1 ? "line " : "lines ");
            sb.Append($"containing \"{_expectedText}\" but found {found}");
        }
        //sb.Append(_atleast > 0 ? "at least " : "exactly ");
        //sb.Append()
        //else
        //    {
        //        Message = $"at least {_atleast} {_atleast = 1 ? "line" : "lines"} containing \"{_expectedText}\" but found {found}";
        //        return false;
        //    }

        if (_exactly > 0)
            if (found == _exactly)
                return true;
            else
            {
                Message = $"   Expected: at least one line containing \"{_expectedText}\"   But none were found";
                return false;
            }

        return false;
    }
}

public class OutputDoesNotContain : OutputCheck
{
    public OutputDoesNotContain(string expectedText) : base(expectedText) { }

    public override bool Matches(string output)
    {
        if (output.Contains(_expectedText))
        {
            Message = $"   Expected: no lines containing \"{_expectedText}\"   But at least one was found";
            return false;
        }

        return true;
    }
}
