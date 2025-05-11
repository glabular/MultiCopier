using MultiCopierWPF.Shared;

namespace MultiCopierWPF.Interfaces;

public interface ISettingsService
{
    Settings Load();

    void Save(Settings settings);
}

