namespace Comander.Dtos
{
    public enum CopyOption
    {
        None,
        OverrideAll,
        OverrideSingle,
        Cancel
    }

    public class CopyOptionDto
    {
        public CopyOptionDto()
        {
            Options = CopyOption.None;
        }

        public CopyOption Options { get; set; }
    }
}
