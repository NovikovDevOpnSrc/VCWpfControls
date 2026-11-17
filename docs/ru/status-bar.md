---
title: Строка состояния
description: Тематические неявные стили StatusBar/StatusBarItem во всех палитрах — текст caption-размера, разделитель 1px сверху, тематические разделители элементов; в шелле демо — живой бар (страница + тема).
---

# Строка состояния

Библиотека темизирует стандартные WPF `StatusBar` и `StatusBarItem` через неявные стили,
мержимые ThemeManager'ом на всё приложение — пишите обычные WPF-строки состояния, и они
следуют Light/Dark/HighContrast.

```xml
<StatusBar>
    <StatusBarItem>
        <TextBlock Text="{Binding Status}" />
    </StatusBarItem>
    <StatusBarItem>
        <!-- разделители элементов — тематические Rectangle 1px -->
        <Rectangle Width="1" Margin="4,6,4,6"
                   Fill="{DynamicResource {x:Static grid:GridResources.SeparatorLineKey}}" />
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <TextBlock Text="{Binding ItemCount}" />
    </StatusBarItem>
</StatusBar>
```

- **Метрики** (по гайдлайнам): текст caption-размера (12) на полосе 28px, фон-поверхность,
  верхний разделитель 1px `SeparatorLineKey`, ритм элементов 8px.
- **Разделители элементов**: у `StatusBarItem` НЕТ `SeparatorStyleKey` в WPF (в отличие
  от `MenuItem`) — обычный `<Separator/>` рисует горизонтальную линию. Используйте
  тематический `Rectangle` 1px как выше.
- **Высокая контрастность**: все кисти идут через токены палитры, алиасящие живые
  системные цвета.
- В шелле демо снизу живёт **строка состояния**: заголовок текущей страницы и имя текущей
  темы, обновляющиеся при навигации и на `ThemeManager.ThemeChanged`.

## Объявление изменений

Строка состояния сообщает о состоянии, меняющемся само по себе — пометьте текстовый
регион live, чтобы вспомогательные технологии объявляли обновления:
`AutomationProperties.LiveSetting="Polite"` (так делает элемент темы в демо).
