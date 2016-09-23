namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    public enum ViewModify
    {
        Seek = -1,  // reposition to current record primary key
        Refresh = 0,  // refetch current record data
        Insert = 1,  // insert new record, fails if matching key exists
        Update = 2,  // update existing non-key data of fetched record
        Assign = 3,  // insert record, replacing any existing record
        Replace = 4,  // update record, delete old if primary key edit
        Merge = 5,  // fails if record with duplicate key not identical
        Delete = 6,  // remove row referenced by this record from table
        InsertTemporary = 7,  // insert a temporary record
        Validate = 8,  // validate a fetched record
        ValidateNew = 9,  // validate a new record
        ValidateField = 10, // validate field(s) of an incomplete record
        ValidateDelete = 11, // validate before deleting record
    }
}