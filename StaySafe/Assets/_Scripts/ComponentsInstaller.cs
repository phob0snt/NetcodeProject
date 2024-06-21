using UnityEngine;
using Zenject;

public class ComponentsInstaller : MonoInstaller
{
    [SerializeField] private FixedJoystick _joystick;
    [SerializeField] private Core _core;
    public override void InstallBindings()
    {
        Container.Bind<FixedJoystick>().FromInstance(_joystick).AsSingle().NonLazy();
        Container.Bind<Core>().FromInstance(_core).AsSingle().NonLazy();
    }
}
