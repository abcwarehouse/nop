#!/bin/bash
if [ "$1" == "-c" ]; then
	isCheck="--check"
    echo "checking for lint errors.."
fi

dotnet format $isCheck --exclude Libraries Presentation Tests/Nop.Core.Tests/ Tests/Nop.Services.Tests Tests/Nop.Tests Tests/Nop.Web.MVC.Tests