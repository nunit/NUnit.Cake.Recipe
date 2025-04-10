// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

CommandLineOptions.Initialize(Context);

public static class CommandLineOptions
{
    static private ICakeContext _context;

    static public ValueOption<string> Target;
	static public MultiValueOption<string> Targets;

    static public ValueOption<string> Configuration;
    static public ValueOption<string> PackageVersion;
    static public ValueOption<string> PackageId;
    static public ValueOption<string> PackageType;
    static public ValueOption<int> TestLevel;
    static public ValueOption<string> TraceLevel;
    static public SimpleOption NoBuild;
    static public SimpleOption NoPush;
    static public SimpleOption Usage;

    public static void Initialize(ICakeContext context)
    {
        _context = context;

        // The name of the TARGET task to be run, e.g. Test.
        Target = new ValueOption<string>("target|t", "Default");

		// Multiple targets to be run
		Targets = new MultiValueOption<string>("target|t", "Default");

        Configuration = new ValueOption<String>("configuration|c", DEFAULT_CONFIGURATION);
		
        PackageVersion = new ValueOption<string>("packageVersion|p", null);

        PackageId = new ValueOption<string>("packageId|id", null);

		PackageType = new ValueOption<string>("packageType|type", null);

        TestLevel = new ValueOption<int>("level|lev", 0);

        TraceLevel = new ValueOption<string>("trace|tr", "Off");

        NoBuild = new SimpleOption("nobuild|nob");

        NoPush = new SimpleOption("nopush|nop");

        Usage = new SimpleOption("usage");
    }

    // Nested classes to represent individual options

    // AbstractOption has a name and can tell us if it exists.
    public abstract class AbstractOption
    {
		public List<string> Aliases { get; }
		
	    public bool Exists 
	    {
		    get
		    {
                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        return true;
                return false;
		    }
	    }

	    public string Description { get; }

	    public AbstractOption(string aliases, string description = null)
	    {
            Aliases = new List<string>(aliases.Split('|'));
            Description = description;
	    }
    }

    // Simple Option adds an implicit boolean conversion operator.
    // It throws an exception if you gave it a value on the command-line.
    public class SimpleOption : AbstractOption
    {
	    static public implicit operator bool(SimpleOption o) => o.Exists;

	    public SimpleOption(string aliases, string description = null)
		    : base(aliases, description)
	    {
            foreach (string alias in Aliases)
                if (_context.Argument(alias, (string)null) != null)
                    throw new Exception($"Option --{alias} does not take a value.");
        }
    }

    // Generic ValueOption adds Value as well as a default value
    public class ValueOption<T> : AbstractOption
    {
	    public T DefaultValue { get; }

	    public ValueOption(string aliases, T defaultValue, string description = null)
		    : base(aliases, description)
	    {
		    DefaultValue = defaultValue;
	    }

	    public T Value
	    {
		    get
		    {
                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        return _context.Argument<T>(alias);

                return DefaultValue;
		    }
	    }
    }

    // Generic MultiValueOption adds Values, which returns a collection of values
    public class MultiValueOption<T> : ValueOption<T>
    {
	    public MultiValueOption(string aliases, T defaultValue, string description = null)
		    : base(aliases, defaultValue, description) { }

		public ICollection<T> Values
		{
			get
			{
				var result = new List<T>();

                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        result.AddRange(_context.Arguments<T>(alias));
                
				if (result.Count == 0) result.Add(DefaultValue);

				return result;
			}
		}
    }
}
