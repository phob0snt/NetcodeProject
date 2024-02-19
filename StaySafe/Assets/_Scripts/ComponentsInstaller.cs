using UnityEngine;
using Zenject;

public class ComponentsInstaller : MonoInstaller
{
    [SerializeField] private FixedJoystick joystick;
    public override void InstallBindings()
    {
        Container.Bind<FixedJoystick>().FromInstance(joystick).AsSingle().NonLazy();
    }
}
