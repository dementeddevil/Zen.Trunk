namespace System.Threading.Tasks.Dataflow
{
    public interface ITaskRequest
    {
        Task Task
        {
            get;
        }
    }
}