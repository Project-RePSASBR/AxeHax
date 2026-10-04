using AxeHax.Core.Verification;

namespace AxeHax.Core.Patching;

public interface IPatcher : IDisposable
{
    public List<Message> Verify();

    public void Patch();
}