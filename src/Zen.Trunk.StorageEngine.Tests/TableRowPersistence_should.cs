using System.Collections.Generic;
using System.IO;
using Xunit;
using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Table Row")]
    public class TableRowPersistence_should
    {
        public List<TableColumnInfo> ColumnDefinition { get; } =
            new List<TableColumnInfo>
            {
                new TableColumnInfo("id", TableColumnDataType.Int, false),
                new TableColumnInfo("name", TableColumnDataType.NVarChar, false, 50),
                new TableColumnInfo("code", TableColumnDataType.Int),
                new TableColumnInfo("block", TableColumnDataType.Int)
            };

        [Fact(DisplayName = nameof(TableRowPersistence_should) + "_" + nameof(read_the_same_data_that_is_written_to_a_stream))]
        public void read_the_same_data_that_is_written_to_a_stream()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new TableRowWriter(stream, ColumnDefinition);
                for (var index = 0; index < 10; ++index)
                {
                    writer[0] = 1 + index;
                    writer[1] = $"Test{index}";
                    writer[2] = 10 + index;
                    writer[3] = 54 + index;
                    writer.Write();
                }

                stream.Flush();
                var writePosition = stream.Position;
                stream.Position = 0;

                var reader = new TableRowReader(stream, ColumnDefinition);
                for (var index = 0; index < 10; ++index)
                {
                    reader.Read();
                    var id = (int)reader[0];
                    var name = (string)reader[1];
                    var code = (int)reader[2];
                    var block = (int)reader[3];

                    Assert.Equal(1 + index, id);
                    Assert.Equal($"Test{index}", name);
                    Assert.Equal(10 + index, code);
                    Assert.Equal(54 + index, block);
                }

                var readerPosition = stream.Position;
                Assert.Equal(writePosition, readerPosition);
            }
        }
    }
}