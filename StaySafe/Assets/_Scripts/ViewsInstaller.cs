using UnityEngine;
using Zenject;

public class ViewsInstaller : MonoInstaller
{
    [SerializeField] private BuilderView _BuilderView;
    public override void InstallBindings()
    {
        Container.Bind<BuilderView>().FromInstance(_BuilderView).AsSingle().NonLazy();
    }
}
