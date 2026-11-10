using System;
using System.Collections.Concurrent;
using System.Linq;
using Avalonia.Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: ComanderTest.FailOnBindingErrors]

namespace ComanderTest
{
    // Bindings are reflection-based, so a typo in a path only shows up as a log warning. Turn it into a test failure.
    public class BindingErrorSink : ILogSink
    {
        public static readonly ConcurrentQueue<string> Errors = new();

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning && area == LogArea.Binding;

        public void Log(LogEventLevel level, string area, object source, string messageTemplate) =>
            Log(level, area, source, messageTemplate, Array.Empty<object>());

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            if (!IsEnabled(level, area)) return;
            var values = propertyValues ?? Array.Empty<object>();
            int i = 0;
            var text = System.Text.RegularExpressions.Regex.Replace(messageTemplate, @"\{[^}]+\}", m => i < values.Length ? Convert.ToString(values[i++]) : m.Value);
            // A null link in the path (e.g. no SelectedFile yet) is normal; typos and type errors are what we want.
            if (text.Contains("Value is null")) return;
            Errors.Enqueue(text + " (source: " + source?.GetType().Name + ")");
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class FailOnBindingErrorsAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test)
        {
            Logger.Sink = new BindingErrorSink();
            BindingErrorSink.Errors.Clear();
        }

        public void AfterTest(ITest test)
        {
            var errors = BindingErrorSink.Errors.Distinct().ToArray();
            BindingErrorSink.Errors.Clear();
            if (errors.Length > 0)
                Assert.Fail("Binding errors:\n" + string.Join("\n", errors));
        }
    }
}
