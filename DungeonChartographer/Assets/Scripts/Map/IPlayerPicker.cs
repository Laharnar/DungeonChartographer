public interface IPlayerPicker
{
    SlotInfo SelectedSlot { get; }
    void OnPickerPicks(object id, PlayerPicker picker);
}
