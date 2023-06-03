using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;

namespace Client.Pages
{
    public partial class Index : IStickyNoteClient
    {
        private List<Note> notes = new();
        private IStickyNoteHub hubProxy = default!;
        private HubConnection connection = default!;

        protected override async Task OnInitializedAsync()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5139/stickynotehub")
                .Build();
            hubProxy = connection.ServerProxy<IStickyNoteHub>();
            _ = connection.ClientRegistration<IStickyNoteClient>(this);
            await connection.StartAsync();

            notes = await hubProxy.LoadNotes();
        }


        public Task NoteCreated(Note note)
        {
            notes.Add(note);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public Task NoteDeleted(Guid id)
        {
            if (notes.FirstOrDefault(n => n.Id == id) is not { } localNote)
                return Task.CompletedTask;

            notes.Remove(localNote);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public Task NoteUpdated(Note note)
        {
            if (notes.FirstOrDefault(n => n.Id == note.Id) is not { } localNote)
                return Task.CompletedTask;

            localNote.Text = note.Text;
            localNote.X = note.X;
            localNote.Y = note.Y;
            localNote.LastLockingUser = note.LastLockingUser;
            localNote.LastEdited = note.LastEdited;

            StateHasChanged();
            return Task.CompletedTask;
        }

        private (double x, double y)? anchor;
        private Note? editNote;

        public async Task Down(Note note, PointerEventArgs eventArgs)
        {
            if (!await hubProxy.LockNote(note.Id)) return;

            note.Lock(connection.ConnectionId);
            anchor = (eventArgs.ClientX, eventArgs.ClientY);
            editNote = note;
        }

        public async Task Move(PointerEventArgs eventArgs)
        {
            if (anchor is not (double x, double y) || editNote is null || !editNote.CanLock(connection.ConnectionId))
                return;

            editNote.X += eventArgs.ClientX - x;
            editNote.Y += eventArgs.ClientY - y;
            editNote.LastEdited = DateTimeOffset.UtcNow;
            anchor = (eventArgs.ClientX, eventArgs.ClientY);
            await hubProxy.MoveNote(editNote.Id, editNote.X, editNote.Y);
        }

        public async Task Up(PointerEventArgs eventArgs)
        {
            await Move(eventArgs);

            anchor = null;
            editNote = null;
        }
    }
}
