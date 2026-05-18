// This file contains both real constants and static readonly variables used
// as constants. All values are initialized before any instance variables.

// Common values used in all NUnit packages
static readonly string[] NUNIT_PACKAGE_AUTHORS = new[] { "Charlie Poole, Rob Prouse" };
static readonly string[] NUNIT_PACKAGE_OWNERS = new[] { "Charlie Poole, Rob Prouse" };
static readonly NuSpecLicense NUNIT_LICENSE = new NuSpecLicense() { Type = "expression", Value = "MIT" };

const string NUNIT_ICON = "nunit_256.png";
// TODO: Automatic update of year
const string NUNIT_COPYRIGHT = "Copyright (c) 2021-2025 Charlie Poole, Rob Prouse";
const string NUNIT_PROJECT_URL = "https://nunit.org/";
const string NUNIT_RAW_URL = "https://raw.githubusercontent.com/nunit/";
const string NUNIT_MAILING_LIST_URL = "https://groups.google.com/forum/#!forum/nunit-discuss";
