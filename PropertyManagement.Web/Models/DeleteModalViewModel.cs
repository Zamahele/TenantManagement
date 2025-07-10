namespace PropertyManagement.Web.Models
{
    public class DeleteModalViewModel
    {
        public string ModalId { get; set; }
        public string ModalLabelId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public int EntityId { get; set; }
        public string? ExtraButtonHtml { get; set; }
    }
}