#region

using System;
using UnityEngine.UIElements;

#endregion

namespace Imperium.Util.Binding;

public class ImpBinaryBinding : ImpBinding<bool>
{
    public event Action onTrue;
    public event Action onFalse;

    public ImpBinaryBinding(
        bool currentValue,
        Action onTrue = null,
        Action onFalse = null
    ) : base(currentValue)
    {
        this.onTrue += onTrue;
        this.onFalse += onFalse;

        onUpdate += OnUpdate;
    }

    public void Toggle() => Set(!Value);
    public void SetTrue() => Set(true);
    public void SetFalse() => Set(false);

    private void OnUpdate(bool updatedValue)
    {
        if (updatedValue)
        {
            onTrue?.Invoke();
        }
        else
        {
            onFalse?.Invoke();
        }
    }

    public static ImpBinaryBinding CreateOr(
        IBinding<bool> binding1,
        IBinding<bool> binding2,
        bool invertBinding1 = false,
        bool invertBinding2 = false
    )
    {
        var binaryBinding = new ImpBinaryBinding(
            invertBinding1 ? !binding1.Value : binding1.Value || invertBinding2 ? !binding2.Value : binding2.Value
        );

        binding1.onUpdate += value =>
        {
            binaryBinding.Set(invertBinding1 ? !value : value || invertBinding2 ? !binding2.Value : binding2.Value);
        };
        binding2.onUpdate += value =>
        {
            binaryBinding.Set(invertBinding2 ? !value : value || invertBinding1 ? !binding1.Value : binding1.Value);
        };

        return binaryBinding;
    }
}