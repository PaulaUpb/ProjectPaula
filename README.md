# Running PAULa

```
cd ProjectPaula/src/ProjectPaula
mkdir userdata
docker build -t paula .
docker run --log-driver=syslog --log-opt tag="paula_docker" -t -d -v userdata:/app/data -p 127.0.0.1:50000:80 paula
```

PAULa is now listening on http://localhost:50000. A database update can be forced by loading http://localhost:50000/Paul/UpdateAllCourses. The loaded page will stay blank.

# PAULa API
PAULa offers a JSON API to retrieve the catalogue from PAUL. You can find its documentation [here](https://gist.github.com/cbruegg/ffc1d42ce74481bbd49dc9aa23b7018a).
