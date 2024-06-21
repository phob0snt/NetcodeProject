using UnityEngine;
using Zenject;

public class TestMonoInstaller : MonoInstaller
{
    [SerializeField] private TestGameObject _testGO;
    public override void InstallBindings()
    {
        Container.Bind<TestGameObject>().FromInstance(_testGO).AsSingle().NonLazy();
    }
}
