namespace Zen.Trunk.Storage.Data.Table
{
    internal abstract class RowConstraintExecute
    {
        public abstract RowConstraintType ConstraintType
        {
            get;
        }

        public abstract void ValidateRow(RowConstraint constraint, TableColumnInfo[] columnDef,
            object[] rowData);
    }
}