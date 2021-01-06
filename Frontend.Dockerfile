FROM nginx:alpine

# Listen ports
EXPOSE 80

COPY spa /usr/share/nginx/html