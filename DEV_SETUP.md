# Development environment setup in Linux with bash

1. Install GitVersion
```
dotnet tool install --global GitVersion.Tool`
```

2. Add tools directory to PATH (in case it isn't already)
```
cat << \EOF >> ~/.bash_profile
# Add .NET Core SDK tools
export PATH="$PATH:~/.dotnet/tools"
EOF
```