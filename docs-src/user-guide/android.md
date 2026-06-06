# Installing on Android

The same UI compiles to a native Android package. There's no Play Store listing yet, so you build
the app yourself (or install an APK someone built for you).

## What you'll need

- An Android device on **Android 6.0 (API 23) or newer**.
- A PC with the **.NET SDK 10**. The Android head targets `net10.0-android`.
- The **.NET Android workload** plus an Android SDK and a JDK. Installing
  [Android Studio](https://developer.android.com/studio) gets you the SDK and JDK; then add the
  workload:

  ```powershell
  dotnet workload install android
  ```

  If a build can't find the Android SDK, set `AndroidSdkDirectory` or the `ANDROID_HOME` environment
  variable.

## Option A — install to a plugged-in phone

1. Unlock **Developer options**: **Settings → About phone**, tap **Build number** seven times.
2. In **Settings → System → Developer options**, turn on **USB debugging**.
3. Connect the phone over USB and accept the **Allow USB debugging?** prompt.
4. From the repository root:

   ```powershell
   dotnet build src\Collectary.UI.Android\Collectary.UI.Android.csproj -t:Install -f net10.0-android
   ```

Collectary then appears in your app drawer.

## Option B — build an APK and sideload it

1. Publish a release build:

   ```powershell
   dotnet publish src\Collectary.UI.Android\Collectary.UI.Android.csproj -c Release -f net10.0-android
   ```

2. The signed APK is at:

   ```
   src\Collectary.UI.Android\bin\Release\net10.0-android\publish\com.collectary.app-Signed.apk
   ```

3. Get the file onto the phone (USB, Drive, email).
4. Tap the APK. Android asks permission to **install from unknown sources** the first time; allow it,
   then confirm.

!!! note "It's signed with a debug key"
    The publish APK is signed with a development key — fine for your own devices, but not a
    store-ready production build, so don't distribute it widely.

## First launch

On first run Collectary creates its local database, runs migrations, and asks you to create an
account. Everything is stored on the device. For the same collections on phone and computer, set up
[Sync](sync.md) through a shared folder.

## Troubleshooting

| Symptom | Likely fix |
|---|---|
| `error XA5300`: Android SDK not found | Install Android Studio, or set `ANDROID_HOME`. |
| Build can't find a JDK | Install the JDK from Android Studio, or set `JAVA_HOME`. |
| Device not detected over USB | Re-accept the USB debugging prompt; try another cable/port; run `adb devices`. |
| "App not installed" when sideloading | Uninstall the old debug-signed copy first, then install the new APK. |
