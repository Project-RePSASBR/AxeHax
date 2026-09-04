using AxeHax.Core.Verification;

namespace AxeHax.Core.Patching;

public interface IPatcher
{
    public List<Message> Verify();

    public void Patch();
}