using System.Text;
using Kerbholz.Cli;

// Standard input as UTF-8 whatever the console's code page is: a message piped in on Windows would otherwise arrive in the OEM code page.
using var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
return CliApp.Run(args, stdin, Console.Out, Console.Error);
