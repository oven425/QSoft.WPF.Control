while ($true) {
    $message = [pscustomobject]@{
        culture   = [System.Globalization.CultureInfo]::CurrentCulture.Name
        timestamp = [DateTimeOffset]::Now.ToString(
            'O',
            [System.Globalization.CultureInfo]::InvariantCulture
        )
    }

    [Console]::Out.WriteLine(
        (ConvertTo-Json -InputObject $message -Compress)
    )
    [Console]::Out.Flush()
    Start-Sleep -Seconds 1
}