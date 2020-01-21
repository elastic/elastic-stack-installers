using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.Extensions;

namespace ElastiBuild.Infra
{
    public class TweakedSentenceBuilder : SentenceBuilder
    {
        public override Func<string> RequiredWord =>
            () => "Required.";

        public override Func<string> ErrorsHeadingText =>
            () => "ERROR(S):";

        public override Func<string> UsageHeadingText =>
            () => "USAGE:";

        public override Func<string> OptionGroupWord =>
            () => "Group";

        public override Func<bool, string> HelpCommandText =>
            isOption => isOption
                ? "Display this help screen."
                : "Display more information on a specific command.";

        public override Func<bool, string> VersionCommandText =>
            _ => "Display version information.";

        public override Func<Error, string> FormatError =>
            error =>
            {
                switch (error.Tag)
                {
                    case ErrorType.BadFormatTokenError:
                        return "Token '".JoinTo(((BadFormatTokenError) error).Token, "' is not recognized.");

                    case ErrorType.MissingValueOptionError:
                        return "Option '".JoinTo(((MissingValueOptionError) error).NameInfo.NameText, "' has no value.");

                    case ErrorType.UnknownOptionError:
                        return "Option '".JoinTo(((UnknownOptionError) error).Token, "' is unknown.");

                    case ErrorType.MissingRequiredOptionError:
                        var errMisssing = ((MissingRequiredOptionError) error);
                        return errMisssing.NameInfo.Equals(NameInfo.EmptyName)
                                   ? "PRODUCT missing."
                                   : "Required option '".JoinTo(errMisssing.NameInfo.NameText, "' is missing.");

                    case ErrorType.BadFormatConversionError:
                        var badFormat = ((BadFormatConversionError) error);
                        return badFormat.NameInfo.Equals(NameInfo.EmptyName)
                                   ? "A target name is defined with a bad format."
                                   : "Option '".JoinTo(badFormat.NameInfo.NameText, "' is defined with a bad format.");

                    case ErrorType.SequenceOutOfRangeError:
                        var seqOutRange = ((SequenceOutOfRangeError) error);
                        return seqOutRange.NameInfo.Equals(NameInfo.EmptyName)
                                   ? "A free sequence value is defined with few items than required."
                                   : "A sequence option '".JoinTo(seqOutRange.NameInfo.NameText,
                                        "' is defined with fewer or more items than required.");

                    case ErrorType.BadVerbSelectedError:
                        return "Command '".JoinTo(((BadVerbSelectedError) error).Token, "' is not recognized.");

                    case ErrorType.NoVerbSelectedError:
                        return "No command selected.";

                    case ErrorType.RepeatedOptionError:
                        return "Option '".JoinTo(((RepeatedOptionError) error).NameInfo.NameText,
                            "' is defined multiple times.");

                    case ErrorType.SetValueExceptionError:
                        var setValueError = (SetValueExceptionError) error;
                        return "Error setting value to option '".JoinTo(setValueError.NameInfo.NameText, "': ", setValueError.Exception.Message);
                }
                throw new InvalidOperationException();
            };

        public override Func<IEnumerable<MutuallyExclusiveSetError>, string> FormatMutuallyExclusiveSetErrors =>
            errors =>
            {
                var bySet = from e in errors
                            group e by e.SetName into g
                            select new { SetName = g.Key, Errors = g.ToList() };

                var msgs = bySet.Select(
                    set =>
                    {
                        var names = string.Join(
                            string.Empty,
                            (from e in set.Errors select "'".JoinTo(e.NameInfo.NameText, "', ")).ToArray());
                        var namesCount = set.Errors.Count();

                        var incompat = string.Join(
                            string.Empty,
                            (from x in
                                 (from s in bySet where !s.SetName.Equals(set.SetName) from e in s.Errors select e)
                                .Distinct()
                             select "'".JoinTo(x.NameInfo.NameText, "', ")).ToArray());

                        return
                            new StringBuilder("Option")
                                    .Append(namesCount > 1 ? "s" : String.Empty)
                                    .Append(": ")
                                    .Append(names.Substring(0, names.Length - 2))
                                    .Append(' ')
                                    .Append(namesCount > 1 ? "are" : "is")
                                    .Append(" not compatible with: ")
                                    .Append(incompat.Substring(0, incompat.Length - 2))
                                    .Append('.')
                                .ToString();
                    }).ToArray();
                return string.Join(Environment.NewLine, msgs);
            };
    }
}
