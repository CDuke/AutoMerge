using System;
using Xunit;

namespace AutoMerge.Tests
{
    public class JsonParserTests
    {
        [Fact]
        public void WhenValueHasNewLine_ShouldCorrectParse()
        {
            var value = string.Format("{0}Patch Back", Environment.NewLine);
            var json = string.Format("{{\"comment_format\": \"{0}\"}}", value);

            var values = JsonParser.ParseJson(json);

            Assert.NotEmpty(values);
            Assert.Equal(value, values["comment_format"]);
        }
    }
}
