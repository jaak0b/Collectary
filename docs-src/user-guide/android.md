# Installing on Android

Collectary isn't only a desktop app — the same UI compiles to a native Android package, so you
can carry your collections around on your phone or tablet. This page walks you through getting it
onto an Android device.

There's no Play Store listing yet, so for now you build the app yourself (or grab an APK someone
built for you) and install it directly. It's less scary than it sounds — two commands and you're
done.

## What you'll need

- An Android device running **Android 6.0 (Marshmallow) or newer**. Collectary's minimum
  supported version is API level 23.
- A PC with the **.NET SDK 10** installed. The Android head targets `net10.0-android`, which is a
  little newer than the desktop head's net8.0, so don't be surprised that the same SDK builds both.
- The **.NET Android workload** plus an Android SDK and a JDK. The fastest way to get all three at
  once is to install [Android Studio](https://developer.android.com/studio) — it bundles the SDK
  and JDK — and then add the .NET workload:

  ```powershell
  dotnet workload install android
  ```

  If `dotnet build` later complains it can't find the Android SDK, point it at your install with an
  `AndroidSdkDirectory` property or the `ANDROID_HOME` environment variable.

## Option A — install straight to a plugged-in phone

This is the smoothest route: build and deploy in a single step over USB.

1. On the phone, unlock **Developer options**: open **Settings → About phone** and tap **Build
   number** seven times. You'll see a "You are now a developer" message.
2. Back in **Settings → System → Developer options**, switch on **USB debugging**.
3. Connect the phone to your PC with a USB cable and accept the **Allow USB debugging?** prompt on
   the phone.
4. From the repository root, run:

   ```powershell
   dotnet build src\Collectary.UI.Android\Collectary.UI.Android.csproj -t:Install -f net10.0-android
   ```

That builds the app and pushes it onto the connected device. When it finishes, Collectary appears
in your app drawer — tap it to launch.

## Option B — build an APK and sideload it

If the phone isn't plugged in, or you want a file you can pass to someone else, build an APK and
copy it over.

1. Publish a release build:

   ```powershell
   dotnet publish src\Collectary.UI.Android\Collectary.UI.Android.csproj -c Release -f net10.0-android
   ```

2. The signed APK lands here:

   ```
   src\Collectary.UI.Android\bin\Release\net10.0-android\publish\com.collectary.app-Signed.apk
   ```

3. Get that file onto the phone — USB transfer, Google Drive, email to yourself, whatever's
   easiest.
4. On the phone, tap the APK. Android will ask permission to **install from unknown sources** the
   first time; allow it for your file manager or browser, then confirm the install.

!!! note "It's signed with a debug key"
    The publish APK is signed with a development key. That's perfectly fine for installing on your
    own devices, but it isn't a store-ready, production-signed build — don't distribute it widely.

## First launch on the phone

Collectary behaves the same as on the desktop: on first run it creates its local database, runs
migrations, and asks you to create a user account. Everything is stored locally on the device. If
you want the same collections on your phone and your computer, set up [Sync](sync.md) so they stay
in step through a shared folder.

## Troubleshooting

| Symptom | Likely fix |
|---|---|
| `error XA5300`: Android SDK not found | Install Android Studio, or set `ANDROID_HOME` to your SDK path. |
| Build can't find a JDK | Install the JDK bundled with Android Studio, or set `JAVA_HOME`. |
| Device not detected over USB | Re-accept the USB debugging prompt; try a different cable/port; run `adb devices` to confirm it's listed. |
| "App not installed" when sideloading | A debug-signed build is already installed — uninstall the old copy first, then install the new APK. |
