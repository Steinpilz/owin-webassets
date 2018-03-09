using AutoFixture;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steinpilz.Owin.WebAssets.Tests
{
    public class WebAssetContentTests
    {
        [Fact]
        public void it_replaces_tokens_in_content()
        {
            var content = WebAssetContent.FromBuffer(Encoding.UTF8.GetBytes("url={url}"));

            var sut = content.Replace(new[] { ("{url}", "123") });
            Encoding.UTF8.GetString(sut.Buffer()).ShouldBe("url=123");
        }

        [Fact]
        public void it_encodes_content()
        {
            //var test = new DeflateStream(new MemoryStream(), CompressionMode.Decompress);

            var content = WebAssetContent.FromBuffer(new Fixture().Create<byte[]>());

            var result = content
                .Encode(ContentEncoding.Deflate)
                .Encode(ContentEncoding.Raw)
                .Encode(ContentEncoding.GZip)
                .Encode(ContentEncoding.Deflate)
                .Encode(ContentEncoding.Deflate)
                .EncodeRaw();

            result.Buffer().ShouldBe(content.Buffer());
        }
    }
}
